using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes collection management endpoints accessible to all authenticated roles and, for read operations, to anonymous users.
/// </summary>
[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonCollectionController : BaseRestController
{
    private readonly ICommonCollectionService _commonCollectionService;

    /// <summary>
    /// Initializes a new <see cref="CommonCollectionController"/> with its required service.
    /// </summary>
    /// <param name="commonCollectionService">The service used to manage collection operations.</param>
    public CommonCollectionController(ICommonCollectionService commonCollectionService)
    {
        _commonCollectionService = commonCollectionService;
    }

    /// <summary>
    /// Creates a new collection owned by the authenticated user.
    /// </summary>
    /// <param name="request">The collection data to create.</param>
    /// <returns>A DTO containing the GUID of the newly created collection.</returns>
    [HttpPost]
    public async Task<ActionResult<CreatedObjectResponseDto>> CreateCollection(
        [FromBody] CreateCollectionRequestDto request)
    {
        return Ok(await _commonCollectionService.CreateCollectionAsync(Authentication, request));
    }

    /// <summary>
    /// Returns a paged list of all collections visible to the caller, including virtual static collections for authenticated users.
    /// </summary>
    /// <param name="filterByName">Optional name fragment to filter collections by.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A paged response containing matching collections and the total count.</returns>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<GetAllCollectionsPagedResponseDto>> GetAllCollectionsAsync(
        [FromQuery] string? filterByName,
        [FromQuery] BaseSortAndPagingDto paging)
    {
        return Ok(await _commonCollectionService.GetAllCollectionsAsync(
            OptionalAuthentication,
            filterByName,
            paging));
    }

    /// <summary>
    /// Returns a paged list of specimens belonging to the specified collection.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection to retrieve specimens from.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A response containing the collection metadata and its paged specimens.</returns>
    [AllowAnonymous]
    [HttpGet("{collectionGuid:guid}/specimens")]
    public async Task<ActionResult<GetAllSpecimenInCollectionResponseDto>> GetAllSpecimensInCollectionAsync(
        Guid collectionGuid,
        [FromQuery] string? filterByName,
        [FromQuery] BaseSortAndPagingDto paging)
    {
        return Ok(await _commonCollectionService.GetAllSpecimensInCollectionAsync(
            OptionalAuthentication,
            collectionGuid,
            filterByName,
            paging));
    }

    /// <summary>
    /// Updates the name or visibility of an existing collection.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection to update.</param>
    /// <param name="request">The new data to apply to the collection.</param>
    [HttpPatch("{collectionGuid:guid}")]
    public async Task<IActionResult> UpdateCollectionAsync(
        Guid collectionGuid,
        [FromBody] UpdateCollectionRequestDto request)
    {
        await _commonCollectionService.UpdateCollectionAsync(Authentication, collectionGuid, request);
        return Ok();
    }

    /// <summary>
    /// Soft-deletes a collection owned by the authenticated user.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection to delete.</param>
    [HttpDelete("{collectionGuid:guid}")]
    public async Task<IActionResult> DeleteCollectionAsync(Guid collectionGuid)
    {
        await _commonCollectionService.DeleteCollectionAsync(Authentication, collectionGuid);
        return Ok();
    }

    /// <summary>
    /// Restores a previously soft-deleted collection to active status.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection to activate.</param>
    [HttpPost("{collectionGuid:guid}/activate")]
    public async Task<IActionResult> ActivateCollectionAsync(Guid collectionGuid)
    {
        await _commonCollectionService.ActivateCollectionAsync(Authentication, collectionGuid);
        return Ok();
    }

    /// <summary>
    /// Adds a specimen to the specified collection.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection to add the specimen to.</param>
    /// <param name="specimenGuid">The GUID of the specimen to add.</param>
    [HttpPost("{collectionGuid}/specimen/{specimenGuid}")]
    public async Task<IActionResult> AddSpecimenToCollectionAsync(Guid collectionGuid, Guid specimenGuid)
    {
        await _commonCollectionService.AddSpecimenToCollectionAsync(Authentication,collectionGuid, specimenGuid);
        return Ok();
    }

    /// <summary>
    /// Removes a specimen from the specified collection.
    /// </summary>
    /// <param name="collectionGuid">The GUID of the collection to remove the specimen from.</param>
    /// <param name="specimenGuid">The GUID of the specimen to remove.</param>
    [HttpDelete("{collectionGuid}/specimen/{specimenGuid}")]
    public async Task<IActionResult> RemoveSpecimenFromCollectionAsync(Guid collectionGuid, Guid specimenGuid)
    {
        await _commonCollectionService.RemoveSpecimenFromCollectionAsync(Authentication, collectionGuid, specimenGuid);
        return Ok();
    }
}
