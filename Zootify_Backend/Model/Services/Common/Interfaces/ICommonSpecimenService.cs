using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines operations for creating, retrieving, and updating specimens and their associated tags and images.
/// </summary>
public interface ICommonSpecimenService
{
    /// <summary>
    /// Creates a new specimen, resolves its geographic location, and persists associated tags and image.
    /// </summary>
    /// <param name="authentication">The authentication context of the creating user.</param>
    /// <param name="request">The specimen creation data.</param>
    /// <returns>A DTO containing the GUID of the newly created specimen.</returns>
    Task<CreatedObjectResponseDto> CreateSpecimenAsync(
        IAuthentication authentication,
        CreateSpecimenRequestDto request);

    /// <summary>
    /// Returns the detailed view of a specimen within a given collection context, including its taxonomy path.
    /// </summary>
    /// <param name="authentication">The optional authentication context used to enforce visibility rules.</param>
    /// <param name="specimenGuid">The GUID of the specimen to retrieve.</param>
    /// <param name="collectionGuid">The GUID of the collection context to validate membership against.</param>
    /// <returns>A detailed specimen response DTO including taxonomy path and audit information.</returns>
    /// <exception cref="ZootifyException">Thrown when no specimen with the given GUID exists.</exception>
    Task<GetSpecimenDetailResponseDto> GetSpecimenInCollectionByGuidAsync(
        IAuthentication? authentication,
        Guid specimenGuid,
        Guid collectionGuid);

    /// <summary>
    /// Returns all tags whose names contain the given fragment, ordered alphabetically.
    /// </summary>
    /// <param name="filter">The name fragment to filter tag names by.</param>
    /// <returns>A list of matching tag filter DTOs.</returns>
    Task<List<GetAllTagsByFilterDto>> GetAllTagsAsync(string filter);

    /// <summary>
    /// Updates the core fields of an existing specimen, verifying ownership.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen to update.</param>
    /// <param name="request">The new data to apply to the specimen.</param>
    /// <exception cref="ZootifyException">Thrown when the object is not in active status.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    Task UpdateSpecimenAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenRequestDto request);

    /// <summary>
    /// Replaces the image attached to a specimen with the provided Base64-encoded image.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen whose image to update.</param>
    /// <param name="request">The new Base64 image data.</param>
    /// <exception cref="ZootifyException">Thrown when the object is not in active status.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    Task UpdateSpecimenImageAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenImageRequestDto request);

    /// <summary>
    /// Replaces the tag associations of a specimen with the provided set of tags, creating new tags as needed.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen whose tags to update.</param>
    /// <param name="request">The new set of tags, which may include both existing and new tags.</param>
    /// <exception cref="ZootifyException">Thrown when the object is not in active status.</exception>
    Task UpdateSpecimenTagsAsync(
        IAuthentication authentication,
        Guid specimenGuid,
        UpdateSpecimenTagsRequestDto request
    );

    /// <summary>
    /// Soft-deletes a specimen after verifying that the caller has ownership rights.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen to delete.</param>
    Task DeleteSpecimenAsync(IAuthentication authentication, Guid specimenGuid);

    /// <summary>
    /// Restores a previously soft-deleted specimen to active status after verifying ownership.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="specimenGuid">The GUID of the specimen to activate.</param>
    Task ActivateSpecimenAsync(IAuthentication authentication, Guid specimenGuid);

    /// <summary>
    /// Returns all specimens belonging to the given taxonomy node or any of its descendant species.
    /// </summary>
    /// <param name="authentication">The optional authentication context used to enforce visibility rules.</param>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node to search under.</param>
    /// <returns>A list of specimen DTOs (including their tags) matching the taxonomy subtree.</returns>
    /// <exception cref="ZootifyException">Thrown when no taxonomy node with the given GUID exists.</exception>
    Task<List<SpecimenWithTagsDto>> GetSpecimensByTaxonomyNodeGuidAsync(IAuthentication? authentication, Guid taxonomyGuid);
}