using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonCollectionController : BaseRestController
{
    private readonly ICommonCollectionService _commonCollectionService;

    public CommonCollectionController(ICommonCollectionService commonCollectionService)
    {
        _commonCollectionService = commonCollectionService;
    }

    [HttpPost]
    public async Task<ActionResult<CreatedObjectResponseDto>> CreateCollection(
        [FromBody] CreateCollectionRequestDto request)
    {
        return Ok(await _commonCollectionService.CreateCollectionAsync(Authentication, request));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<GetCollectionResponseDto>>> GetPublicCollectionsAsync()
    {
        return Ok(await _commonCollectionService.GetPublicCollectionsAsync());
    }

    [HttpPatch("{collectionGuid:guid}")]
    public async Task<IActionResult> UpdateCollectionAsync(
        Guid collectionGuid,
        [FromBody] UpdateCollectionRequestDto request)
    {
        await _commonCollectionService.UpdateCollectionAsync(Authentication, collectionGuid, request);
        return Ok();
    }

    [HttpDelete("{collectionGuid:guid}")]
    public async Task<IActionResult> DeleteCollectionAsync(Guid collectionGuid)
    {
        await _commonCollectionService.DeleteCollectionAsync(Authentication, collectionGuid);
        return Ok();
    }
    
    [HttpPost("activate/{collectionGuid:guid}")]
    public async Task<IActionResult> ActivateCollectionAsync(Guid collectionGuid)
    {
        await _commonCollectionService.ActivateCollectionAsync(Authentication, collectionGuid);
        return Ok();
    }

    [HttpPost("{collectionGuid}/specimen/{specimenGuid}")]
    public async Task<IActionResult> AddSpecimenToCollection(Guid collectionGuid, Guid specimenGuid)
    {
        await _commonCollectionService.AddSpecimenToCollectionAsync(Authentication,collectionGuid, specimenGuid);
        return Ok();
    }

    [HttpDelete("{collectionGuid}/specimen/{specimenGuid}")]
    public async Task<IActionResult> RemoveSpecimenFromCollection(Guid collectionGuid, Guid specimenGuid)
    {
        await _commonCollectionService.RemoveSpecimenFromCollectionAsync(Authentication, collectionGuid, specimenGuid);
        return Ok();
    }
}
