using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

[Authorize]
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
        return Ok(await _commonSpecimenService.CreateSpecimenAsync(specimen));
    }
}