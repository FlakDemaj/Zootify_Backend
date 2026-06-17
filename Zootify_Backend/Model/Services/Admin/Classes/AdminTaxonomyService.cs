using AutoMapper;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Providers.SignalR;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

/// <summary>
/// Handles taxonomy moderation business logic including approval and rejection of pending nodes.
/// </summary>
public class AdminTaxonomyService : IAdminTaxonomyService
{
    /// <summary>The minimum name similarity (0..1) for a taxonomy to be offered as a similar match.</summary>
    private const double SimilarityThreshold = 0.5;

    /// <summary>The maximum number of similar taxonomies returned as a decision aid.</summary>
    private const int MaxSimilarResults = 10;

    private readonly ITaxonomyRepository _taxonomyRepository;
    private readonly ISpecimenRepository _specimenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBrevoProvider _brevoProvider;
    private readonly INotificationProvider _notificationProvider;
    private readonly IPostCommitQueue _postCommitQueue;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="AdminTaxonomyService"/> with its required dependencies.
    /// </summary>
    /// <param name="taxonomyRepository">Repository for taxonomy node persistence operations.</param>
    /// <param name="specimenRepository">Repository for specimen persistence operations.</param>
    /// <param name="userRepository">Repository for user lookups.</param>
    /// <param name="brevoProvider">Email provider for sending taxonomy notification emails.</param>
    /// <param name="notificationProvider">Provider for sending in-app push notifications.</param>
    /// <param name="postCommitQueue">Queue used to dispatch notifications only after the transaction commits.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    public AdminTaxonomyService(
        ITaxonomyRepository taxonomyRepository,
        ISpecimenRepository specimenRepository,
        IUserRepository userRepository,
        IBrevoProvider brevoProvider,
        INotificationProvider notificationProvider,
        IPostCommitQueue postCommitQueue,
        IMapper mapper)
    {
        _taxonomyRepository = taxonomyRepository;
        _specimenRepository = specimenRepository;
        _userRepository = userRepository;
        _brevoProvider = brevoProvider;
        _notificationProvider = notificationProvider;
        _postCommitQueue = postCommitQueue;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns the taxonomy tree filtered to only include subtrees containing at least one pending node.
    /// </summary>
    /// <returns>A taxonomy tree response DTO containing all pending subtrees.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<GetTaxonomyTreeResponseDto> GetTaxonomiesForApprovalAsync()
    {
        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var byParent = allNodes.ToLookup(n => n.ParentGuid);

        var roots = byParent[null]
            .Where(r => SubtreeContainsPending(r.Guid, byParent))
            .OrderBy(r => r.Name)
            .Select(r => BuildTreeNode(r, byParent))
            .ToList();

        return _mapper.Map<GetTaxonomyTreeResponseDto>(roots);
    }

    /// <summary>
    /// Returns existing approved taxonomies of the same rank whose name sounds similar to the node
    /// under review, as a decision aid when approving or rejecting it.
    /// </summary>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node being reviewed.</param>
    /// <returns>The reviewed node together with its most similar existing approved taxonomies.</returns>
    /// <exception cref="ZootifyException">Thrown when no taxonomy node with the given GUID exists.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<GetSimilarTaxonomiesResponseDto> GetSimilarTaxonomiesAsync(Guid taxonomyGuid)
    {
        var node = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(taxonomyGuid);

        if (node == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyFound);
        }

        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var byGuid = allNodes.ToDictionary(n => n.Guid);

        // Compare only against already-approved taxonomies of the same rank (a duplicate would sit at
        // the same level); the reviewed node itself is excluded by GUID.
        var similar = allNodes
            .Where(n => n.Guid != node.Guid
                        && n.Rank == node.Rank
                        && n.ApprovalStatus == ApprovalStatus.Approved)
            .Select(n => new SimilarTaxonomyDto
            {
                TaxonomyGuid = n.Guid,
                Name = n.Name,
                Rank = n.Rank,
                ApprovalStatus = n.ApprovalStatus,
                Similarity = Math.Round(FuzzyMatch.Similarity(node.Name, n.Name), 4),
                Path = BuildAncestorPath(n, byGuid),
            })
            .Where(s => s.Similarity >= SimilarityThreshold)
            .OrderByDescending(s => s.Similarity)
            .ThenBy(s => s.Name)
            .Take(MaxSimilarResults)
            .ToList();

        return new GetSimilarTaxonomiesResponseDto
        {
            TaxonomyGuid = node.Guid,
            Name = node.Name,
            Rank = node.Rank,
            Similar = similar,
        };
    }

    /// <summary>
    /// Approves a pending taxonomy node, activates specimens whose full taxonomy chain is now approved, and notifies affected users.
    /// </summary>
    /// <param name="authentication">The authentication context of the approving moderator or admin.</param>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node to approve.</param>
    /// <exception cref="ZootifyException">Thrown when no taxonomy node with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when the taxonomy node is already approved.</exception>
    [Transaction(ReadOnly = false)]
    public async Task ApproveTaxonomyAsync(IAuthentication authentication, Guid taxonomyGuid)
    {
        var node = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(taxonomyGuid);

        if (node == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyFound);
        }

        if (node.ApprovalStatus == ApprovalStatus.Approved)
        {
            throw new ZootifyException(ErrorCode.TaxonomyAlreadyApproved);
        }

        node.ApprovalStatus = ApprovalStatus.Approved;
        node.ApprovedByUserGuid = authentication.UserGuid;
        node.ApprovedDate = DateTime.UtcNow;
        await _taxonomyRepository.UpdateAsync(node);

        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var byGuid = allNodes.ToDictionary(n => n.Guid);
        // Reflect the approval in the in-memory snapshot so that IsFullChainApproved,
        // which runs immediately after, sees the updated status without an extra DB round-trip.
        if (byGuid.TryGetValue(taxonomyGuid, out var cached))
        {
            cached.ApprovalStatus = ApprovalStatus.Approved;
        }

        var subtreeSpeciesGuids = CollectSubtreeSpeciesGuids(taxonomyGuid, allNodes);

        var affectedUserGuids = new HashSet<Guid>();
        if (node.CreatedByUserGuid.HasValue)
        {
            affectedUserGuids.Add(node.CreatedByUserGuid.Value);
        }

        if (subtreeSpeciesGuids.Count > 0)
        {
            var pendingSpecimens = await _specimenRepository
                .GetSpecimensBySpeciesGuidsAsync(subtreeSpeciesGuids, status: SpecimenStatus.Pending);

            var toActivate = pendingSpecimens
                .Where(s => s.TaxonomySpeciesGuid.HasValue
                            && IsFullChainApproved(s.TaxonomySpeciesGuid.Value, byGuid))
                .ToList();

            foreach (var specimen in toActivate)
            {
                specimen.Status = SpecimenStatus.Active;
            }

            if (toActivate.Count > 0)
            {
                await _specimenRepository.MassUpdateAsync(toActivate);
            }

            foreach (var specimen in toActivate)
            {
                if (specimen.CreatedByUserGuid.HasValue)
                {
                    affectedUserGuids.Add(specimen.CreatedByUserGuid.Value);
                }
            }
        }

        var affectedUserGuidList = affectedUserGuids.ToList();
        var users = await _userRepository.GetUsersByGuidsAsync(affectedUserGuidList);

        // Each affected user is notified by email and/or in-app push depending on their settings, each
        // in their own preferred language. Dispatched only after the transaction commits so no one is
        // notified about an approval that was rolled back.
        var approvedName = node.Name;
        var approvedRank = node.Rank;
        _postCommitQueue.Enqueue(() => ParallelNotifications.RunAsync(users, async user =>
        {
            if (user.Settings.SendEmail)
            {
                await _brevoProvider.SendTaxonomyApprovedEmail(
                    user.Email, user.FirstName, user.LastName, approvedName, approvedRank,
                    user.Settings.Language);
            }

            if (user.Settings.SendPushNotification)
            {
                await _notificationProvider.SendNotificationAsync(
                    user,
                    $"Ihre Taxonomie \"{approvedName}\" wurde genehmigt.",
                    $"Your taxonomy \"{approvedName}\" has been approved.");
            }
        }));
    }

    /// <summary>
    /// Rejects a taxonomy node, hard-deletes its entire subtree, rejects all associated specimens, and notifies affected users.
    /// </summary>
    /// <param name="authentication">The authentication context of the rejecting moderator or admin.</param>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node to reject.</param>
    /// <param name="request">Contains the rejection comment to send to affected users.</param>
    /// <exception cref="ZootifyException">Thrown when no taxonomy node with the given GUID exists.</exception>
    [Transaction(ReadOnly = false)]
    public async Task RejectTaxonomyAsync(
        IAuthentication authentication,
        Guid taxonomyGuid,
        RejectTaxonomyRequestDto request)
    {
        var node = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(taxonomyGuid);

        if (node == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyFound);
        }

        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var subtreeNodes = CollectSubtreeNodesDeepestFirst(taxonomyGuid, allNodes);
        var subtreeSpeciesGuids = subtreeNodes
            .Where(n => n.Rank == TaxonomyRank.Species)
            .Select(n => n.Guid)
            .ToList();

        var affectedUserGuids = new HashSet<Guid>();
        if (node.CreatedByUserGuid.HasValue)
        {
            affectedUserGuids.Add(node.CreatedByUserGuid.Value);
        }

        if (subtreeSpeciesGuids.Count > 0)
        {
            var specimens = await _specimenRepository
                .GetSpecimensBySpeciesGuidsAsync(subtreeSpeciesGuids);

            var toReject = specimens.Where(s => s.Status != SpecimenStatus.Rejected).ToList();
            var rejectedAt = DateTime.UtcNow;

            foreach (var specimen in toReject)
            {
                specimen.Status = SpecimenStatus.Rejected;
                specimen.RejectedAt = rejectedAt;

                if (specimen.CreatedByUserGuid.HasValue)
                {
                    affectedUserGuids.Add(specimen.CreatedByUserGuid.Value);
                }
            }

            if (toReject.Count > 0)
            {
                await _specimenRepository.MassUpdateAsync(toReject);
            }
        }

        // Hard-delete the entire subtree (deepest first to satisfy FK Restrict on ParentGuid).
        // The SetNull cascade on specimens.species_guid runs as part of this delete.
        await _taxonomyRepository.MassDeleteAsync(subtreeNodes);

        var affectedUserGuidList = affectedUserGuids.ToList();
        var users = await _userRepository.GetUsersByGuidsAsync(affectedUserGuidList);

        // Each affected user is notified by email and/or in-app push depending on their settings, each
        // in their own preferred language. Dispatched only after the transaction commits so no one is
        // notified about a rejection (and subtree deletion) that was rolled back.
        var rejectedName = node.Name;
        var rejectedRank = node.Rank;
        var rejectionComment = request.Comment;
        _postCommitQueue.Enqueue(() => ParallelNotifications.RunAsync(users, async user =>
        {
            if (user.Settings.SendEmail)
            {
                await _brevoProvider.SendTaxonomyRejectedEmail(
                    user.Email, user.FirstName, user.LastName, rejectedName, rejectedRank, rejectionComment,
                    user.Settings.Language);
            }

            if (user.Settings.SendPushNotification)
            {
                await _notificationProvider.SendNotificationAsync(
                    user,
                    $"Ihre Taxonomie \"{rejectedName}\" wurde abgelehnt.",
                    $"Your taxonomy \"{rejectedName}\" has been rejected.");
            }
        }));
    }

    /// <summary>
    /// Recursively checks whether the subtree rooted at the given node contains at least one pending taxonomy node.
    /// </summary>
    /// <param name="nodeGuid">The GUID of the root node to check.</param>
    /// <param name="byParent">Lookup of nodes keyed by their parent GUID.</param>
    /// <returns><c>true</c> if any descendant node has a pending approval status; otherwise <c>false</c>.</returns>
    private static bool SubtreeContainsPending(
        Guid nodeGuid,
        ILookup<Guid?, TaxonomyNodeEntity> byParent)
    {
        var children = byParent[nodeGuid].ToList();
        foreach (var child in children)
        {
            if (child.ApprovalStatus == ApprovalStatus.Pending)
            {
                return true;
            }

            if (SubtreeContainsPending(child.Guid, byParent))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Builds the ancestor path of a node from the root down to its parent, joined by " &gt; ".
    /// </summary>
    /// <param name="node">The node whose ancestor path to build.</param>
    /// <param name="byGuid">Dictionary of all taxonomy nodes keyed by GUID.</param>
    /// <returns>The parent chain as a single string, or an empty string for a root node.</returns>
    private static string BuildAncestorPath(
        TaxonomyNodeEntity node,
        Dictionary<Guid, TaxonomyNodeEntity> byGuid)
    {
        var ancestors = new List<string>();
        var current = node.ParentGuid.HasValue
            ? byGuid.GetValueOrDefault(node.ParentGuid.Value)
            : null;

        while (current != null)
        {
            ancestors.Add(current.Name);
            current = current.ParentGuid.HasValue
                ? byGuid.GetValueOrDefault(current.ParentGuid.Value)
                : null;
        }

        ancestors.Reverse();
        return string.Join(" > ", ancestors);
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
        TaxonomyGuid = node.Guid,
        Name = node.Name,
        Rank = node.Rank,
        ApprovalStatus = node.ApprovalStatus,
        Children = byParent[node.Guid]
            .OrderBy(c => c.Name)
            .Select(c => BuildTreeNode(c, byParent))
            .ToList(),
    };

    /// <summary>
    /// Performs a BFS over the subtree rooted at the given GUID and returns the GUIDs of all species-rank nodes found.
    /// </summary>
    /// <param name="rootGuid">The GUID of the subtree root node.</param>
    /// <param name="allNodes">The complete flat list of all taxonomy nodes.</param>
    /// <returns>A list of GUIDs for all species-rank nodes within the subtree.</returns>
    private static List<Guid> CollectSubtreeSpeciesGuids(
        Guid rootGuid,
        List<TaxonomyNodeEntity> allNodes)
    {
        var byParent = allNodes.ToLookup(n => n.ParentGuid);
        var speciesGuids = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootGuid);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var node = allNodes.FirstOrDefault(n => n.Guid == current);

            if (node?.Rank == TaxonomyRank.Species)
            {
                speciesGuids.Add(current);
            }

            foreach (var child in byParent[current])
            {
                queue.Enqueue(child.Guid);
            }
        }

        return speciesGuids;
    }

    /// <summary>
    /// Collects all nodes in the subtree rooted at the given GUID in BFS order then reverses the list so deepest nodes come first.
    /// </summary>
    /// <param name="rootGuid">The GUID of the subtree root node.</param>
    /// <param name="allNodes">The complete flat list of all taxonomy nodes.</param>
    /// <returns>A list of <see cref="TaxonomyNodeEntity"/> instances ordered deepest-first to satisfy foreign-key delete constraints.</returns>
    private static List<TaxonomyNodeEntity> CollectSubtreeNodesDeepestFirst(
        Guid rootGuid,
        List<TaxonomyNodeEntity> allNodes)
    {
        var byParent = allNodes.ToLookup(n => n.ParentGuid);
        var result = new List<TaxonomyNodeEntity>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootGuid);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var node = allNodes.FirstOrDefault(n => n.Guid == current);
            if (node != null)
            {
                result.Add(node);
            }

            foreach (var child in byParent[current])
            {
                queue.Enqueue(child.Guid);
            }
        }

        // Reverse BFS order → deepest nodes first; satisfies FK Restrict on ParentGuid.
        result.Reverse();
        return result;
    }

    /// <summary>
    /// Walks the ancestor chain from the given species node to the root and returns <c>true</c> only if every node in the chain is approved.
    /// </summary>
    /// <param name="speciesGuid">The GUID of the species-rank node to check.</param>
    /// <param name="byGuid">Dictionary of all taxonomy nodes keyed by GUID.</param>
    /// <returns><c>true</c> if every ancestor including the species node is approved; otherwise <c>false</c>.</returns>
    private static bool IsFullChainApproved(
        Guid speciesGuid,
        Dictionary<Guid, TaxonomyNodeEntity> byGuid)
    {
        var current = byGuid.GetValueOrDefault(speciesGuid);
        while (current != null)
        {
            if (current.ApprovalStatus != ApprovalStatus.Approved)
            {
                return false;
            }

            current = current.ParentGuid.HasValue
                ? byGuid.GetValueOrDefault(current.ParentGuid.Value)
                : null;
        }
        return true;
    }
}