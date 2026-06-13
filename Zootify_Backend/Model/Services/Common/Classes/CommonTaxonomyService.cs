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
                throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
            }
        }

        var needsApproval = authentication.Role == UserRole.User;
        
        var nodesToCreate = new List<TaxonomyNodeEntity>();
        
        foreach (var taxonomyNode in sortedTaxonomy)
        {
            var node = _mapper.Map<TaxonomyNodeEntity>(taxonomyNode);
            node.SetCreatedBy(authentication.UserGuid);
            node.ParentGuid = parentGuid;
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
    /// Returns all taxonomy nodes of the specified rank that match the given name prefix, each accompanied by their ancestor path.
    /// </summary>
    /// <param name="startsWith">Optional name prefix to filter nodes by.</param>
    /// <param name="rank">The taxonomy rank to filter by.</param>
    /// <returns>A list of taxonomy chains, where each chain runs from root to the matching node.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<List<List<GetAllTaxonomyNodesByFilterDto>>> GetAllTaxonomyNodesAsync(
        string? startsWith,
        TaxonomyRank rank)
    {
        var taxonomyNodes = await
            _taxonomyRepository
                .GetTaxonomyNodeByStartsWithNameAsync(startsWith, rank);

        var taxonomyTrees = new List<List<GetAllTaxonomyNodesByFilterDto>>();

        foreach (var taxonomyNode in taxonomyNodes)
        {
            var taxonomyTreeList = new List<TaxonomyNodeEntity>();

            var taxonomyTree = await GetTaxonomyTreeAsync(
                taxonomyNode, taxonomyTreeList);

            var mappedTaxonomyTree = _mapper.Map<List<GetAllTaxonomyNodesByFilterDto>>(taxonomyTree);

            mappedTaxonomyTree = mappedTaxonomyTree
                .OrderBy(x => x.Rank)
                .ToList();

            taxonomyTrees.Add(mappedTaxonomyTree);
        }

        return taxonomyTrees;
    }
    
    /// <summary>
    /// Returns the full taxonomy tree, optionally filtered by a root-node name fragment.
    /// </summary>
    /// <param name="filterByName">Optional name fragment to filter root nodes by.</param>
    /// <returns>A hierarchical taxonomy tree response DTO.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<GetTaxonomyTreeResponseDto> GetAllTaxonomyNodesAsTreeAsync(string? filterByName)
    {
        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var byParent = allNodes.ToLookup(n => n.ParentGuid);

        var roots = byParent[null].AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filterByName))
        {
            roots = roots.Where(n => n.Name.Contains(filterByName, StringComparison.OrdinalIgnoreCase));
        }

        var nodes = roots
            .OrderBy(n => n.Name)
            .Select(r => BuildTreeNode(r, byParent))
            .ToList();

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
            throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
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
}