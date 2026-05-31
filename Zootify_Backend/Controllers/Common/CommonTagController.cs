using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

public class CommonTagController : BaseRestController
{
    private readonly ICommonSpecimenService _commonSpecimenService;

    public CommonTagController(ICommonSpecimenService commonSpecimenService)
    {
        _commonSpecimenService = commonSpecimenService;
    }

    [HttpGet]
    public async Task<ActionResult> GetAllTagsByFilter(
        [FromQuery] string startsWith)
    {
        return Ok(await _commonSpecimenService.GetAllTagsAsync(startsWith));
    }
}