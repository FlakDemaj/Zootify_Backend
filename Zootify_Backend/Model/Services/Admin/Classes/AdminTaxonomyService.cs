using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend;
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

    public AdminTaxonomyService(
        ITaxonomyRepository taxonomyRepository,
        ISpecimenRepository specimenRepository,
        IUserRepository userRepository,
        IBrevoProvider brevoProvider)
    {
        _taxonomyRepository = taxonomyRepository;
        _specimenRepository = specimenRepository;
        _userRepository = userRepository;
        _brevoProvider = brevoProvider;
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

        return new GetTaxonomyTreeResponseDto { TotalCount = roots.Count, Nodes = roots };
    }

    [Transaction(ReadOnly = false)]
    public async Task ApproveTaxonomyAsync(IAuthentication authentication, Guid taxonomyGuid)
    {
        var node = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(taxonomyGuid);

        if (node == null)
            throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
        if (node.ApprovalStatus == ApprovalStatus.Approved)
            throw new ZootifyException(ErrorCode.TaxonomyAlreadyApproved);

        node.ApprovalStatus = ApprovalStatus.Approved;
        node.ApprovedByUserGuid = authentication.UserGuid;
        node.ApprovedDate = DateTime.UtcNow;
        await _taxonomyRepository.UpdateAsync(node);

        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var byGuid = allNodes.ToDictionary(n => n.Guid);
        if (byGuid.TryGetValue(taxonomyGuid, out var cached))
            cached.ApprovalStatus = ApprovalStatus.Approved;

        var subtreeSpeciesGuids = CollectSubtreeSpeciesGuids(taxonomyGuid, allNodes);

        var affectedUserGuids = new HashSet<Guid>();
        if (node.CreatedByUserGuid.HasValue)
            affectedUserGuids.Add(node.CreatedByUserGuid.Value);

        if (subtreeSpeciesGuids.Count > 0)
        {
            var pendingSpecimens = await _specimenRepository
                .GetSpecimensBySpeciesGuidsAsync(subtreeSpeciesGuids, SpecimenStatus.Pending);

            var toActivate = pendingSpecimens
                .Where(s => IsFullChainApproved(s.TaxonomySpeciesGuid, byGuid))
                .ToList();

            foreach (var specimen in toActivate)
                specimen.Status = SpecimenStatus.Active;

            if (toActivate.Count > 0)
                await _specimenRepository.MassUpdateAsync(toActivate);

            foreach (var specimen in toActivate)
                if (specimen.CreatedByUserGuid.HasValue)
                    affectedUserGuids.Add(specimen.CreatedByUserGuid.Value);
        }

        var users = await _userRepository.GetUsersByGuidsAsync(affectedUserGuids.ToList());
        foreach (var user in users)
            await _brevoProvider.SendTaxonomyApprovedEmail(
                user.Email, user.FirstName, user.LastName, node.Name, node.Rank);
    }

    [Transaction(ReadOnly = false)]
    public async Task RejectTaxonomyAsync(
        IAuthentication authentication,
        Guid taxonomyGuid,
        RejectTaxonomyRequestDto request)
    {
        var node = await _taxonomyRepository.GetTaxonomyNodeByGuidAsync(taxonomyGuid);

        if (node == null)
            throw new ZootifyException(ErrorCode.NoTaxonomyWithThisGuid);
        if (node.ApprovalStatus == ApprovalStatus.Rejected)
            throw new ZootifyException(ErrorCode.TaxonomyAlreadyRejected);

        node.ApprovalStatus = ApprovalStatus.Rejected;
        await _taxonomyRepository.UpdateAsync(node);

        var allNodes = await _taxonomyRepository.GetAllNodesForTreeAsync();
        var subtreeSpeciesGuids = CollectSubtreeSpeciesGuids(taxonomyGuid, allNodes);

        var affectedUserGuids = new HashSet<Guid>();
        if (node.CreatedByUserGuid.HasValue)
            affectedUserGuids.Add(node.CreatedByUserGuid.Value);

        if (subtreeSpeciesGuids.Count > 0)
        {
            var specimens = await _specimenRepository
                .GetSpecimensBySpeciesGuidsAsync(subtreeSpeciesGuids);

            var toReject = specimens.Where(s => s.Status != SpecimenStatus.Rejected).ToList();

            foreach (var specimen in toReject)
            {
                specimen.Status = SpecimenStatus.Rejected;
                if (specimen.CreatedByUserGuid.HasValue)
                    affectedUserGuids.Add(specimen.CreatedByUserGuid.Value);
            }

            if (toReject.Count > 0)
                await _specimenRepository.MassUpdateAsync(toReject);
        }

        var users = await _userRepository.GetUsersByGuidsAsync(affectedUserGuids.ToList());
        foreach (var user in users)
            await _brevoProvider.SendTaxonomyRejectedEmail(
                user.Email, user.FirstName, user.LastName, node.Name, node.Rank, request.Comment);
    }

    private static bool SubtreeContainsPending(
        Guid nodeGuid,
        ILookup<Guid?, TaxonomyNodeEntity> byParent)
    {
        var children = byParent[nodeGuid].ToList();
        foreach (var child in children)
        {
            if (child.ApprovalStatus == ApprovalStatus.Pending)
                return true;
            if (SubtreeContainsPending(child.Guid, byParent))
                return true;
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
                speciesGuids.Add(current);

            foreach (var child in byParent[current])
                queue.Enqueue(child.Guid);
        }

        return speciesGuids;
    }

    private static bool IsFullChainApproved(
        Guid speciesGuid,
        Dictionary<Guid, TaxonomyNodeEntity> byGuid)
    {
        var current = byGuid.GetValueOrDefault(speciesGuid);
        while (current != null)
        {
            if (current.ApprovalStatus != ApprovalStatus.Approved)
                return false;
            current = current.ParentGuid.HasValue
                ? byGuid.GetValueOrDefault(current.ParentGuid.Value)
                : null;
        }
        return true;
    }
}
