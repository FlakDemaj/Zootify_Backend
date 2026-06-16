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
}