using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonSpecimenController : BaseRestController
{
    private readonly ICommonSpecimenService _commonSpecimenService;

    public CommonSpecimenController(ICommonSpecimenService commonSpecimenService)
    {
        _commonSpecimenService = commonSpecimenService;
    }

    [HttpPost]
    public async Task<ActionResult<CreatedObjectResponseDto>> CreateSpecimen(
        [FromBody] CreateSpecimenRequestDto specimen)
    {
        return Ok(await _commonSpecimenService.CreateSpecimenAsync(Authentication, specimen));
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<GetAllSpecimenDto>>> GetAllSpecimenAsync()
    {
        return Ok(await _commonSpecimenService.GetAllSpecimensByStatusAsync(
            Authentication,
            SpecimenStatus.Active));
    }
    
    [HttpGet("pending")]
    public async Task<ActionResult<List<GetAllSpecimenDto>>> GetAllSpecimenAsync(
        Guid collectionGuid)
    {
        return Ok(await _commonSpecimenService.GetAllSpecimensByStatusAsync(
            Authentication,
            SpecimenStatus.Pending));
    }
    
    [HttpGet("deleted")]
    public async Task<ActionResult<List<GetAllSpecimenDto>>> GetAllDeletedSpecimenAsync()
    {
        return Ok(await _commonSpecimenService.GetAllSpecimensByStatusAsync(
            Authentication,
            SpecimenStatus.Deleted));
    }
    
    [HttpGet("own")]
    public async Task<ActionResult<List<GetAllSpecimenDto>>> GetAllOwnSpecimenAsync()
    {
        return Ok(await _commonSpecimenService.GetAllSpecimensByStatusAsync(
            Authentication,
            SpecimenStatus.Deleted));
    }

    [HttpPatch("{specimenGuid:guid}")]
    public async Task<IActionResult> UpdateSpecimenAsync(
        Guid specimenGuid,
        [FromBody] UpdateSpecimenRequestDto request)
    {
        await _commonSpecimenService.UpdateSpecimenAsync(Authentication, specimenGuid, request);
        return Ok();
    }
    
    [HttpPatch("image/{specimenGuid:guid}")]
    public async Task<IActionResult> UpdateSpecimenImageAsync(
        Guid specimenGuid,
        [FromBody] UpdateSpecimenImageRequestDto request)
    {
        await _commonSpecimenService.UpdateSpecimenImageAsync(Authentication, specimenGuid, request);
        return Ok();
    }
    
    [HttpPatch("tags/{specimenGuid:guid}")]
    public async Task<IActionResult> UpdateSpecimenTagsAsync(
        Guid specimenGuid,
        [FromBody] UpdateSpecimenTagsRequestDto request)
    {
        await _commonSpecimenService.UpdateSpecimenTagsAsync(Authentication, specimenGuid, request);
        return Ok();
    }
}