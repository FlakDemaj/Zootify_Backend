using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with specimen-specific query and count operations.
/// </summary>
public interface ISpecimenRepository : IBaseRepository<SpecimenEntity>
{
    /// <summary>
    /// Returns the specimen with the given GUID, optionally loading image and tag data, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the specimen to retrieve.</param>
    /// <param name="includeImage">Whether to eagerly load the specimen's image.</param>
    /// <param name="includeTags">Whether to eagerly load the specimen's tag associations.</param>
    Task<SpecimenEntity?> GetSpecimenByGuidAsync(
        Guid guid,
        bool includeImage = false,
        bool includeTags = false);

    /// <summary>
    /// Returns the specimen with the given GUID, eagerly loading all detail navigation properties, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the specimen to retrieve.</param>
    Task<SpecimenEntity?> GetSpecimenWithDetailByGuidAsync(Guid guid);

    /// <summary>
    /// Returns the number of specimens owned by the authenticated user that have the given status.
    /// </summary>
    /// <param name="status">The specimen status to count.</param>
    /// <param name="authentication">The authentication context of the user whose specimens are counted.</param>
    Task<int> CountByStatusAsync(SpecimenStatus status, IAuthentication authentication);

    /// <summary>
    /// Returns the total number of specimens created by the given user.
    /// </summary>
    /// <param name="userGuid">The GUID of the user whose specimens are counted.</param>
    Task<int> CountOwnAsync(Guid userGuid);

    /// <summary>
    /// Returns the total number of public specimens across all users.
    /// </summary>
    Task<int> CountPublicAsync();

    /// <summary>
    /// Returns a paged list of specimens belonging to the specified collection, applying visibility rules.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection to retrieve specimens from.</param>
    /// <param name="filterByName">The filter for the specimens in the collections</param>
    /// <param name="authentication">The optional authentication context used for visibility filtering.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A tuple containing the page of specimens and the total matching count.</returns>
    Task<(List<SpecimenEntity> Items, int TotalCount)> GetSpecimensInCollectionPagedAsync(
        Guid collectionGuid,
        string? filterByName,
        IAuthentication? authentication,
        BaseSortAndPagingDto paging);

    /// <summary>
    /// Returns a paged list of specimens, optionally filtered by owner and status.
    /// </summary>
    /// <param name="userGuid">Optional GUID of the owning user; if <c>null</c>, only public specimens are returned.</param>
    /// <param name="filterByName">The filter for the specimens in the collections</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A tuple containing the page of specimens and the total matching count.</returns>
    Task<(List<SpecimenEntity> Items, int TotalCount)> GetSpecimensPagedAsync(
        Guid? userGuid,
        string? filterByName,
        SpecimenStatus? status,
        BaseSortAndPagingDto paging);

    /// <summary>
    /// Returns a paged list of specimens for the public overview, ordered by creation date descending,
    /// eagerly loading the image, taxonomy chain, and geographic location chain. Non-public specimens
    /// are only included for moderators and admins; an optional city name filters the result.
    /// </summary>
    /// <param name="authentication">The optional authentication context; anonymous or regular users see only public specimens.</param>
    /// <param name="filterByCity">Optional case-insensitive city name to filter by; <c>null</c> for no filter.</param>
    /// <param name="paging">Paging parameters (page and page size).</param>
    /// <returns>A tuple containing the page of specimens and the total matching count.</returns>
    Task<(List<SpecimenEntity> Items, int TotalCount)> GetPublicOverviewPagedAsync(
        IAuthentication? authentication,
        string? filterByCity,
        BaseSortAndPagingDto paging);

    /// <summary>
    /// Returns the specimens with the given GUIDs, eagerly loading their species, for export.
    /// </summary>
    /// <param name="guids">The GUIDs of the specimens to load.</param>
    /// <returns>The matching specimens with their species loaded.</returns>
    Task<List<SpecimenEntity>> GetSpecimensByGuidsWithSpeciesAsync(List<Guid> guids);

    /// <summary>
    /// Returns all specimens whose taxonomy species GUID is in the given list, applying optional visibility and status filters.
    /// </summary>
    /// <param name="speciesGuids">The list of species GUIDs to match against.</param>
    /// <param name="authentication">The optional authentication context used for visibility filtering.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="includeTags">When <c>true</c>, eagerly loads each specimen's tags.</param>
    Task<List<SpecimenEntity>> GetSpecimensBySpeciesGuidsAsync(
        List<Guid> speciesGuids,
        IAuthentication? authentication = null,
        SpecimenStatus? status = null,
        bool includeTags = false);

    /// <summary>
    /// Returns a batch of soft-deleted specimens whose deletion date is older than the given cutoff.
    /// </summary>
    /// <param name="cutoff">The maximum allowed deletion date.</param>
    /// <param name="batchSize">The maximum number of specimens to return.</param>
    Task<List<SpecimenEntity>> GetDeletedSpecimensForPurgeAsync(DateTime cutoff, int batchSize);

    /// <summary>
    /// Returns a batch of rejected specimens whose rejection date is older than the given cutoff.
    /// </summary>
    /// <param name="cutoff">The maximum allowed rejection date.</param>
    /// <param name="batchSize">The maximum number of specimens to return.</param>
    Task<List<SpecimenEntity>> GetRejectedSpecimensForPurgeAsync(DateTime cutoff, int batchSize);
}