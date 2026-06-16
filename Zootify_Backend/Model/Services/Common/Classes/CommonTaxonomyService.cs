using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.SignalR;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

/// <summary>
/// Handles business logic for creating and querying taxonomy nodes.
/// </summary>
public class CommonTaxonomyService : ICommonTaxonomyNodeService
{
    private readonly ITaxonomyRepository  _taxonomyRepository;
    
    private readonly IUserRepository _userRepository;
    
    private readonly INotificationProvider _notificationProvider;
    
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="CommonTaxonomyService"/> with its required dependencies.
    /// </summary>
    /// <param name="taxonomyRepository">Repository for taxonomy node persistence operations.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    /// <param name="userRepository">Repository for user lookups used for notification targeting.</param>
    /// <param name="notificationProvider">Provider for sending real-time notifications to admins and moderators.</param>
    public CommonTaxonomyService(
            ITaxonomyRepository taxonomyRepository,
            IMapper mapper,
            IUserRepository userRepository,
            INotificationProvider notificationProvider)
    {
        _taxonomyRepository = taxonomyRepository;
        _mapper = mapper;
        _userRepository = userRepository;
        _notificationProvider = notificationProvider;
    }

    /// <summary>
    /// Creates one or more taxonomy nodes in a chain, optionally rooted under an existing parent node, and notifies admins if approval is needed.
    /// </summary>
    /// <param name="authentication">The authentication context of the creating user.</param>
    /// <param name="requestDto">The taxonomy node chain to create, including an optional parent GUID.</param>
    /// <returns>A list of DTOs containing the GUIDs of the created nodes.</returns>
    /// <exception cref="ZootifyException">Thrown when no taxonomy node with the given GUID exists.</exception>
    [Transaction(ReadOnly = false)]
    public async Task<List<CreatedObjectResponseDto>> CreateTaxonomyNodeAsync(
        IAuthentication authentication,
        CreateTaxonomyNodeRequestDto requestDto)
    {
        var sortedTaxonomy = requestDto.TaxonomyNodes.OrderBy(tn => tn.Key).ToDictionary();

        var parentGuid = requestDto.ParentGuid;

        if (parentGuid != null)
        {
            var parent = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(parentGuid.Value);

            if (parent == null)
            {
                throw new ZootifyException(ErrorCode.NoTaxonomyFound);
            }
        }

        var needsApproval = authentication.Role == UserRole.User;
        
        var nodesToCreate = new List<TaxonomyNodeEntity>();
        
        foreach (var taxonomyNode in sortedTaxonomy)
        {
            var node = _mapper.Map<TaxonomyNodeEntity>(taxonomyNode);
            node.SetCreatedBy(authentication.UserGuid);
            node.ParentGuid = parentGuid;
            // Each newly created node becomes the parent of the next, building a linear chain.
            parentGuid = node.Guid;

            if (!needsApproval)
            {
                node.ApprovalStatus = ApprovalStatus.Approved;
            }
            
            nodesToCreate.Add(node);
        }
        
        var createdNodes = await _taxonomyRepository.MassCreateAsync(nodesToCreate);

        await SendNotificationAsync(needsApproval);
        
        return _mapper.Map<List<CreatedObjectResponseDto>>(createdNodes);
    }

    /// <summary>
    /// Returns all active or pending species-rank taxonomy nodes that match the given name fragment, as a flat list.
    /// </summary>
    /// <param name="filter">Optional name fragment to filter species by.</param>
    /// <returns>A flat list of matching species nodes, ordered by name.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<List<GetSpeciesForCreationDto>> GetAllActiveOrPendingSpeciesNodesForCreationAsync(
        string? filter)
    {
        // Load all active/pending species and apply fuzzy-augmented matching in memory so typos still
        // resolve (e.g. "Panthera leoo" finds "Panthera leo") on top of plain substring matches.
        var species = await _taxonomyRepository.GetActiveOPendingSpeciesNodeByNameFilterAsync(null);

        var matched = FuzzyMatch.SearchAugmented(filter, species, s => s.Name);

        return _mapper.Map<List<GetSpeciesForCreationDto>>(matched);
    }
    
    /// <summary>
    /// Returns the taxonomy tree, optionally filtered by a name prefix that may match a node at any
    /// depth. When filtering, only the branches leading to a matching node are kept (non-matching
    /// sibling branches are pruned), while the full subtree below each match is included. Roots without
    /// any match are omitted.
    /// </summary>
    /// <param name="filterByName">Optional name prefix; only branches containing a starts-with match are returned.</param>
    /// <returns>A hierarchical taxonomy tree response DTO.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<GetTaxonomyTreeResponseDto> GetAllTaxonomyNodesAsTreeAsync(string? filterByName)
    {
        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var byParent = allNodes.ToLookup(n => n.ParentGuid);

        var roots = byParent[null].OrderBy(n => n.Name);

        List<GetTaxonomyTreeNodeDto> nodes;
        if (string.IsNullOrWhiteSpace(filterByName))
        {
            nodes = roots.Select(r => BuildTreeNode(r, byParent)).ToList();
        }
        else
        {
            nodes = roots
                .Select(r => BuildFilteredTreeNode(r, byParent, filterByName))
                .OfType<GetTaxonomyTreeNodeDto>()
                .ToList();
        }

        return new GetTaxonomyTreeResponseDto
        {
            TotalCount = nodes.Count,
            Nodes      = nodes,
        };
    }

    /// <summary>
    /// Recursively walks up the parent chain from the given node to the root, collecting all ancestor nodes.
    /// </summary>
    /// <param name="taxonomyNode">The starting taxonomy node.</param>
    /// <param name="taxonomyNodes">The accumulator list that will contain the full ancestor chain.</param>
    /// <returns>The accumulated list of <see cref="TaxonomyNodeEntity"/> instances from leaf to root.</returns>
    /// <exception cref="ZootifyException">Thrown when no taxonomy node with the given GUID exists.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<List<TaxonomyNodeEntity>> GetTaxonomyTreeAsync(
        TaxonomyNodeEntity taxonomyNode,
        List<TaxonomyNodeEntity> taxonomyNodes)
    {
        taxonomyNodes.Add(taxonomyNode);

        if (taxonomyNode.ParentGuid == null)
        {
            return taxonomyNodes;
        }

        var parentTaxonomy = await _taxonomyRepository
            .GetTaxonomyNodeByGuidAsync((Guid)taxonomyNode.ParentGuid);

        if (parentTaxonomy == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyFound);
        }

        return await GetTaxonomyTreeAsync(parentTaxonomy, taxonomyNodes);
    }
    
    /// <summary>
    /// Sends a real-time notification to all admins and moderators if the newly created nodes require approval.
    /// </summary>
    /// <param name="needsApproval">Whether the created nodes require moderator approval.</param>
    private async Task SendNotificationAsync(bool needsApproval)
    {
        if (!needsApproval)
        {
            return;
        }
        
        var users = await _userRepository.GetAllAdminsAndModeratorUsersWithSettingsAsync();
        
        _ = _notificationProvider.SendNotificationAsync(users);
    }
    
    /// <summary>
    /// Recursively builds a <see cref="GetTaxonomyTreeNodeDto"/> for the given node and all its descendants.
    /// </summary>
    /// <param name="node">The taxonomy node to build a tree DTO for.</param>
    /// <param name="byParent">Lookup of nodes keyed by their parent GUID.</param>
    /// <returns>A populated <see cref="GetTaxonomyTreeNodeDto"/> including all child nodes.</returns>
    private static GetTaxonomyTreeNodeDto BuildTreeNode(
        TaxonomyNodeEntity node,
        ILookup<Guid?, TaxonomyNodeEntity> byParent) => new()
    {
        TaxonomyGuid   = node.Guid,
        Name           = node.Name,
        Rank           = node.Rank,
        ApprovalStatus = node.ApprovalStatus,
        Children = byParent[node.Guid]
            .OrderBy(c => c.Name)
            .Select(c => BuildTreeNode(c, byParent))
            .ToList(),
    };

    /// <summary>
    /// Builds a tree node pruned to the given name prefix: a node whose name matches is returned with
    /// its full subtree; a non-matching node is kept only if it has a matching descendant (and then only
    /// the branches leading to them); otherwise <c>null</c> is returned. A name matches when any of its
    /// whitespace-separated words starts with the filter, so "leo" finds "Panthera leo" while an inner
    /// substring (e.g. the "in" inside "Cyprinus") does not pull in an unrelated branch.
    /// </summary>
    /// <param name="node">The taxonomy node to evaluate.</param>
    /// <param name="byParent">Lookup of nodes keyed by their parent GUID.</param>
    /// <param name="filter">The word prefix to match (case-insensitive).</param>
    /// <returns>The pruned tree node, or <c>null</c> if neither the node nor any descendant matches.</returns>
    private static GetTaxonomyTreeNodeDto? BuildFilteredTreeNode(
        TaxonomyNodeEntity node,
        ILookup<Guid?, TaxonomyNodeEntity> byParent,
        string filter)
    {
        // A node whose name matches the prefix is returned with its complete subtree.
        if (NameMatchesPrefix(node.Name, filter))
        {
            return BuildTreeNode(node, byParent);
        }

        // Otherwise keep the node only if a descendant matches, pruning the non-matching sibling branches.
        var children = byParent[node.Guid]
            .OrderBy(c => c.Name)
            .Select(c => BuildFilteredTreeNode(c, byParent, filter))
            .OfType<GetTaxonomyTreeNodeDto>()
            .ToList();

        if (children.Count == 0)
        {
            return null;
        }

        return new GetTaxonomyTreeNodeDto
        {
            TaxonomyGuid   = node.Guid,
            Name           = node.Name,
            Rank           = node.Rank,
            ApprovalStatus = node.ApprovalStatus,
            Children       = children,
        };
    }

    /// <summary>
    /// Returns <c>true</c> if any whitespace-separated word of <paramref name="name"/> starts with the
    /// given filter (case-insensitive). This lets a filter match a single word inside a binomial name
    /// (e.g. "leo" matches "Panthera leo") without matching an inner substring (e.g. "in" in "Cyprinus").
    /// </summary>
    /// <param name="name">The taxonomy node name.</param>
    /// <param name="filter">The word prefix to match.</param>
    private static bool NameMatchesPrefix(string name, string filter)
    {
        return name
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Any(word => word.StartsWith(filter, StringComparison.OrdinalIgnoreCase));
    }
}