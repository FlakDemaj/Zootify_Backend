using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.DTOs.Common.Response.Helpers;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes specimen management endpoints accessible to all authenticated roles and, for read operations, to anonymous users.
/// </summary>
[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonSpecimenController : BaseRestController
{
    private readonly ICommonSpecimenService _commonSpecimenService;

    /// <summary>
    /// Initializes a new <see cref="CommonSpecimenController"/> with its required service.
    /// </summary>
    /// <param name="commonSpecimenService">The service used to manage specimen operations.</param>
    public CommonSpecimenController(ICommonSpecimenService commonSpecimenService)
    {
        _commonSpecimenService = commonSpecimenService;
    }

    /// <summary>
    /// Returns the detailed view of a specimen within a given collection context.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen to retrieve.</param>
    /// <param name="collectionGuid">The GUID of the collection context to validate membership against.</param>
    /// <returns>A detailed specimen response DTO including taxonomy path and audit information.</returns>
    [AllowAnonymous]
    [HttpGet("{collectionGuid:guid}/{specimenGuid:guid}")]
    public async Task<ActionResult<GetSpecimenDetailResponseDto>> GetSpecimenDetailInCollectionAsync(
        Guid specimenGuid,
        Guid collectionGuid)
    {
        return Ok(await _commonSpecimenService.GetSpecimenInCollectionByGuidAsync(
            OptionalAuthentication,
            specimenGuid,
            collectionGuid));
    }

    /// <summary>
    /// Returns all specimens belonging to the given taxonomy node or any of its descendant species,
    /// each including the tags assigned to the specimen.
    /// </summary>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node to search under.</param>
    /// <returns>A list of specimen DTOs (with tags) matching the taxonomy subtree.</returns>
    [AllowAnonymous]
    [HttpGet("{taxonomyGuid:guid}")]
    public async Task<ActionResult<List<SpecimenWithTagsDto>>> GetSpecimensByTaxonomyGuidAsync(Guid taxonomyGuid)
    {
        return Ok(await _commonSpecimenService.GetSpecimensByTaxonomyNodeGuidAsync(
            OptionalAuthentication,
            taxonomyGuid));
    }

    /// <summary>
    /// Creates a new specimen and associates it with the resolved geographic location and provided tags.
    /// </summary>
    /// <param name="specimen">The data required to create the specimen.</param>
    /// <returns>A DTO containing the GUID of the newly created specimen.</returns>
    [HttpPost]
    public async Task<ActionResult<CreatedObjectResponseDto>> CreateSpecimenAsync(
        [FromBody] CreateSpecimenRequestDto specimen)
    {
        return Ok(await _commonSpecimenService.CreateSpecimenAsync(Authentication, specimen));
    }

    /// <summary>
    /// Soft-deletes a specimen owned by the authenticated user.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen to delete.</param>
    [HttpDelete("{specimenGuid:guid}")]
    public async Task<IActionResult> DeleteSpecimenAsync(Guid specimenGuid)
    {
        await _commonSpecimenService.DeleteSpecimenAsync(Authentication, specimenGuid);
        return Ok();
    }

    /// <summary>
    /// Restores a previously soft-deleted specimen to active status.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen to activate.</param>
    [HttpPost("{specimenGuid:guid}/activate")]
    public async Task<IActionResult> ActivateSpecimenAsync(Guid specimenGuid)
    {
        await _commonSpecimenService.ActivateSpecimenAsync(Authentication, specimenGuid);
        return Ok();
    }

    /// <summary>
    /// Updates the core fields of an existing specimen.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen to update.</param>
    /// <param name="request">The new data to apply to the specimen.</param>
    [HttpPatch("{specimenGuid:guid}")]
    public async Task<IActionResult> UpdateSpecimenAsync(
        Guid specimenGuid,
        [FromBody] UpdateSpecimenRequestDto request)
    {
        await _commonSpecimenService.UpdateSpecimenAsync(Authentication, specimenGuid, request);
        return Ok();
    }

    /// <summary>
    /// Replaces the image attached to a specimen with the provided Base64-encoded image.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen whose image to update.</param>
    /// <param name="request">The new Base64 image data.</param>
    [HttpPatch("{specimenGuid:guid}/image")]
    public async Task<IActionResult> UpdateSpecimenImageAsync(
        Guid specimenGuid,
        [FromBody] UpdateSpecimenImageRequestDto request)
    {
        await _commonSpecimenService.UpdateSpecimenImageAsync(Authentication, specimenGuid, request);
        return Ok();
    }

    /// <summary>
    /// Replaces the tag associations of a specimen with the provided set of tags.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen whose tags to update.</param>
    /// <param name="request">The new set of tags, which may include both existing and new tags.</param>
    [HttpPatch("{specimenGuid:guid}/tags")]
    public async Task<IActionResult> UpdateSpecimenTagsAsync(
        Guid specimenGuid,
        [FromBody] UpdateSpecimenTagsRequestDto request)
    {
        await _commonSpecimenService.UpdateSpecimenTagsAsync(Authentication, specimenGuid, request);
        return Ok();
    }
}