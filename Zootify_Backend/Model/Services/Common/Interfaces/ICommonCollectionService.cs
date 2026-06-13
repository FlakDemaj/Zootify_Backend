using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines operations for managing specimen collections, including virtual static collections.
/// </summary>
public interface ICommonCollectionService
{
    /// <summary>
    /// Creates a new collection owned by the authenticated user.
    /// </summary>
    /// <param name="authentication">The authentication context of the creating user.</param>
    /// <param name="request">The collection creation data.</param>
    /// <returns>A DTO containing the GUID of the newly created collection.</returns>
    Task<CreatedObjectResponseDto> CreateCollectionAsync(
        IAuthentication authentication,
        CreateCollectionRequestDto request);

    /// <summary>
    /// Returns a paged list of all collections visible to the caller, including virtual static collections for authenticated users.
    /// </summary>
    /// <param name="authentication">The optional authentication context; anonymous callers see only public active collections.</param>
    /// <param name="filterByName">Optional name fragment to filter collections by.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A paged response containing matching collections and the total count.</returns>
    Task<GetAllCollectionsPagedResponseDto> GetAllCollectionsAsync(
        IAuthentication? authentication,
        string? filterByName,
        BaseSortAndPagingDto paging);

    /// <summary>
    /// Returns a paged list of specimens belonging to the specified collection.
    /// </summary>
    /// <param name="authentication">The optional authentication context used to enforce visibility rules.</param>
    /// <param name="collectionGuid">The GUID of the collection to retrieve specimens from.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A response containing the collection metadata and its paged specimens.</returns>
    Task<GetAllSpecimenInCollectionResponseDto> GetAllSpecimensInCollectionAsync(
        IAuthentication? authentication,
        Guid collectionGuid,
        BaseSortAndPagingDto paging);

    /// <summary>
    /// Adds a specimen to the specified collection, verifying ownership of the collection.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to add the specimen to.</param>
    /// <param name="specimenGuid">The GUID of the specimen to add.</param>
    Task AddSpecimenToCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        Guid specimenGuid);

    /// <summary>
    /// Updates the name or visibility of an existing collection, verifying ownership.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to update.</param>
    /// <param name="request">The new data to apply to the collection.</param>
    Task UpdateCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        UpdateCollectionRequestDto request);

    /// <summary>
    /// Soft-deletes a collection, verifying that the caller is the owner.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to delete.</param>
    Task DeleteCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid);

    /// <summary>
    /// Restores a previously soft-deleted collection to active status.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to activate.</param>
    Task ActivateCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid);

    /// <summary>
    /// Removes a specimen from the specified collection, verifying ownership of the collection.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to remove the specimen from.</param>
    /// <param name="specimenGuid">The GUID of the specimen to remove.</param>
    Task RemoveSpecimenFromCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        Guid specimenGuid);
}
