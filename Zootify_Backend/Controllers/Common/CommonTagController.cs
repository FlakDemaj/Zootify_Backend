using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
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