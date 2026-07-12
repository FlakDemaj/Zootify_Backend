using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="TaxonomyNodeEntity"/> records.
/// </summary>
public class TaxonomyRepository : BaseRepository<TaxonomyNodeEntity>, ITaxonomyRepository
{
    /// <summary>
    /// Initializes a new <see cref="TaxonomyRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public TaxonomyRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns the taxonomy node with the given GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the taxonomy node to retrieve.</param>
    public async Task<TaxonomyNodeEntity?> GetTaxonomyNodeByGuidAsync(Guid guid)
    {
        return await Context
            .GetTrackingSet<TaxonomyNodeEntity>()
            .Where(t => t.Guid == guid)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns all active or pending species-rank taxonomy nodes whose
    /// name contains the given fragment, or all species if no filter is provided.
    /// </summary>
    /// <param name="filter">Optional name fragment to filter node names.</param>
    /// <returns>The matching <see cref="TaxonomyNodeEntity"/> records.</returns>
    public async Task<List<TaxonomyNodeEntity>> GetActiveOPendingSpeciesNodeByNameFilterAsync(
        string? filter)
    {
        var query = Context
            .GetTrackingSet<TaxonomyNodeEntity>()
            .Where(tn => tn.Rank == TaxonomyRank.Species
            && (tn.ApprovalStatus == ApprovalStatus.Approved 
                || tn.ApprovalStatus == ApprovalStatus.Pending));

        if (!string.IsNullOrWhiteSpace(filter))
        {
            // Trim so stray leading/trailing whitespace doesn't break the match; spaces inside the
            // filter are kept, so e.g. "Panthera leo" matches the full binomial name.
            var trimmedFilter = filter.Trim();
            query = query
                .Where(tn => tn.Name.Contains(trimmedFilter));
        }

        return await query.ToListAsync();
    }

    /// <summary>
    /// Returns all taxonomy nodes ordered by rank then name, for use in tree construction.
    /// </summary>
    /// <returns>All <see cref="TaxonomyNodeEntity"/> records ordered by rank and name.</returns>
    public async Task<List<TaxonomyNodeEntity>> GetAllNodesForTreeAsync()
    {
        return await Context
            .GetTrackingSet<TaxonomyNodeEntity>()
            .OrderBy(t => t.Rank)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Returns a batch of rejected leaf taxonomy nodes whose rejection date is older than the given
    /// cutoff. Only leaves (nodes without children) are returned so the delete never violates the
    /// foreign-key <c>Restrict</c> on <c>parent_guid</c>; a rejected parent is purged in a later batch
    /// once its children have been removed.
    /// </summary>
    /// <param name="cutoff">The exclusive upper bound for the rejection date.</param>
    /// <param name="batchSize">The maximum number of nodes to return.</param>
    public async Task<List<TaxonomyNodeEntity>> GetRejectedTaxonomyLeafNodesForPurgeAsync(
        DateTime cutoff,
        int batchSize)
    {
        return await Context.GetTrackingSet<TaxonomyNodeEntity>()
            .Where(n => n.ApprovalStatus == ApprovalStatus.Rejected
                && n.RejectedDate != null
                && n.RejectedDate < cutoff
                && !Context.Set<TaxonomyNodeEntity>().Any(child => child.ParentGuid == n.Guid))
            .OrderBy(n => n.RejectedDate)
            .Take(batchSize)
            .ToListAsync();
    }
}