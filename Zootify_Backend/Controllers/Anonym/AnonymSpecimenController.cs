using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Anonym;

[AllowAnonymous]
public class AnonymSpecimenController : BaseRestController
{
    private readonly ICommonSpecimenService _commonSpecimenService;

    public AnonymSpecimenController(ICommonSpecimenService commonSpecimenService)
    {
        _commonSpecimenService = commonSpecimenService;
    }

    [AllowAnonymous]
    [HttpGet("{specimenGuid:guid}")]
    public async Task<ActionResult<GetSpecimenDetailResponseDto>> GetSpecimenDetailAsync(
        Guid specimenGuid,
        [FromQuery] Guid collectionGuid)
    {
        return Ok(await _commonSpecimenService.GetSpecimenByGuidAsync(
            null,
            specimenGuid,
            collectionGuid));
    }
}
