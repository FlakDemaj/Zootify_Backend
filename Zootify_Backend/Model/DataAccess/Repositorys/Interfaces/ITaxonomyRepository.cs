using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with taxonomy node query operations.
/// </summary>
public interface ITaxonomyRepository : IBaseRepository<TaxonomyNodeEntity>
{
    /// <summary>
    /// Returns the taxonomy node with the given GUID, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the taxonomy node to retrieve.</param>
    Task<TaxonomyNodeEntity?> GetTaxonomyNodeByGuidAsync(Guid guid);

    /// <summary>
    /// Returns all species-rank taxonomy nodes whose names contain the given fragment.
    /// </summary>
    /// <param name="filter">Optional name fragment to filter nodes by.</param>
    Task<List<TaxonomyNodeEntity>> GetActiveOPendingSpeciesNodeByNameFilterAsync(
        string? filter);

    /// <summary>
    /// Returns all taxonomy nodes ordered by rank and name, used to build the full taxonomy tree.
    /// </summary>
    Task<List<TaxonomyNodeEntity>> GetAllNodesForTreeAsync();

    /// <summary>
    /// Returns a batch of rejected leaf taxonomy nodes (nodes without children) whose rejection date is
    /// older than the given cutoff, for purging. Restricting to leaves keeps the delete within the
    /// foreign-key <c>Restrict</c> on <c>parent_guid</c>; parents become eligible once their children are gone.
    /// </summary>
    /// <param name="cutoff">The exclusive upper bound for the rejection date.</param>
    /// <param name="batchSize">The maximum number of nodes to return.</param>
    Task<List<TaxonomyNodeEntity>> GetRejectedTaxonomyLeafNodesForPurgeAsync(DateTime cutoff, int batchSize);
}