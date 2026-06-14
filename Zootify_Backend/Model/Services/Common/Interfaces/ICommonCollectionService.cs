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
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
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
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    /// <exception cref="ZootifyException">Thrown when no specimen with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when the specimen is already in the collection.</exception>
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
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    /// <exception cref="ZootifyException">Thrown when the object is not in active status.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    Task UpdateCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        UpdateCollectionRequestDto request);

    /// <summary>
    /// Soft-deletes a collection, verifying that the caller is the owner.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to delete.</param>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    Task DeleteCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid);

    /// <summary>
    /// Restores a previously soft-deleted collection to active status.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to activate.</param>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    Task ActivateCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid);

    /// <summary>
    /// Removes a specimen from the specified collection, verifying ownership of the collection.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="collectionGuid">The GUID of the collection to remove the specimen from.</param>
    /// <param name="specimenGuid">The GUID of the specimen to remove.</param>
    /// <exception cref="ZootifyException">Thrown when the collection is not found.</exception>
    /// <exception cref="ZootifyException">Thrown when the specimen is not in the collection.</exception>
    Task RemoveSpecimenFromCollectionAsync(
        IAuthentication authentication,
        Guid collectionGuid,
        Guid specimenGuid);
}
