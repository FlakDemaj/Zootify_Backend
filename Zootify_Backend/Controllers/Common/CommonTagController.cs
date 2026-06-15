using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes tag search endpoints accessible to all authenticated roles.
/// </summary>
[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonTagController : BaseRestController
{
    private readonly ICommonSpecimenService _commonSpecimenService;

    /// <summary>
    /// Initializes a new <see cref="CommonTagController"/> with its required service.
    /// </summary>
    /// <param name="commonSpecimenService">The service used to query tags.</param>
    public CommonTagController(ICommonSpecimenService commonSpecimenService)
    {
        _commonSpecimenService = commonSpecimenService;
    }

    /// <summary>
    /// Returns all tags whose names contain the given fragment, ordered alphabetically.
    /// </summary>
    /// <param name="filter">The name fragment to filter tag names by.</param>
    /// <returns>A list of matching tag DTOs.</returns>
    [HttpGet]
    public async Task<ActionResult> GetAllTagsByFilter(
        [FromQuery] string filter)
    {
        return Ok(await _commonSpecimenService.GetAllTagsAsync(filter));
    }
}