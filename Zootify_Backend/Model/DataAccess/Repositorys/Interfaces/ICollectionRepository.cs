using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with collection-specific query operations.
/// </summary>
public interface ICollectionRepository : IBaseRepository<CollectionEntity>
{
    /// <summary>
    /// Returns a filtered list of collections visible to the caller, applying authentication-based visibility rules and name filtering.
    /// </summary>
    /// <param name="authentication">The optional authentication context used for visibility filtering.</param>
    /// <param name="filterByName">Optional name fragment to filter collections by.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A list of matching <see cref="CollectionEntity"/> instances.</returns>
    Task<List<CollectionEntity>> GetAllCollectionsByNameFilterAsync(
        IAuthentication? authentication,
        string? filterByName,
        BaseSortAndPagingDto paging);

    /// <summary>
    /// Returns the collection with the given GUID, including its specimen-collection links, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the collection to retrieve.</param>
    Task<CollectionEntity?> GetCollectionByGuidAsync(Guid guid);

    /// <summary>
    /// Returns the collection with the given GUID, eagerly loading audit user navigation properties, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the collection to retrieve.</param>
    Task<CollectionEntity?> GetCollectionWithAuditAsync(Guid guid);

    /// <summary>
    /// Returns a batch of soft-deleted collections whose deletion date is older than the given cutoff.
    /// </summary>
    /// <param name="cutoff">The maximum allowed deletion date; collections deleted before this date are eligible for purge.</param>
    /// <param name="batchSize">The maximum number of collections to return.</param>
    Task<List<CollectionEntity>> GetDeletedCollectionsForPurgeAsync(DateTime cutoff, int batchSize);
}
