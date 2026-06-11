using AutoMapper;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Services.Admin.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Admin.Classes;

public class AdminTaxonomyService : IAdminTaxonomyService
{
    private readonly ITaxonomyRepository _taxonomyRepository;
    private readonly ISpecimenRepository _specimenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IBrevoProvider _brevoProvider;
    private readonly IMapper _mapper;

    public AdminTaxonomyService(
        ITaxonomyRepository taxonomyRepository,
        ISpecimenRepository specimenRepository,
        IUserRepository userRepository,
        IBrevoProvider brevoProvider, 
        IMapper mapper)
    {
        _taxonomyRepository = taxonomyRepository;
        _specimenRepository = specimenRepository;
        _userRepository = userRepository;
        _brevoProvider = brevoProvider;
        _mapper = mapper;
    }

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

    [Transaction(ReadOnly = false)]
    public async Task ApproveTaxonomyAsync(IAuthentication authentication, Guid taxonomyGuid)
    {
        var node = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(taxonomyGuid);

        if (node == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
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
                .GetSpecimensBySpeciesGuidsAsync(subtreeSpeciesGuids, SpecimenStatus.Pending);

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

        var users = await _userRepository.GetUsersByGuidsAsync(affectedUserGuids.ToList());
        foreach (var user in users)
        {
            await _brevoProvider.SendTaxonomyApprovedEmail(
                user.Email, user.FirstName, user.LastName, node.Name, node.Rank);
        }
    }

    [Transaction(ReadOnly = false)]
    public async Task RejectTaxonomyAsync(
        IAuthentication authentication,
        Guid taxonomyGuid,
        RejectTaxonomyRequestDto request)
    {
        var node = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(taxonomyGuid);

        if (node == null)
        {
            throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
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

        var users = await _userRepository.GetUsersByGuidsAsync(affectedUserGuids.ToList());
        foreach (var user in users)
        {
            await _brevoProvider.SendTaxonomyRejectedEmail(
                user.Email, user.FirstName, user.LastName, node.Name, node.Rank, request.Comment);
        }
    }

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