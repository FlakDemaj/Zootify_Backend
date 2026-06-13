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
    /// Returns all taxonomy nodes of the given rank whose names start with the given prefix.
    /// </summary>
    /// <param name="startsWithName">Optional name prefix to filter nodes by.</param>
    /// <param name="rank">The taxonomy rank to filter by.</param>
    Task<List<TaxonomyNodeEntity>> GetTaxonomyNodeByStartsWithNameAsync(
        string? startsWithName,
        TaxonomyRank rank);

    /// <summary>
    /// Returns all taxonomy nodes ordered by rank and name, used to build the full taxonomy tree.
    /// </summary>
    Task<List<TaxonomyNodeEntity>> GetAllNodesForTreeAsync();
}