using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Home;

/// <summary>
/// Exposes public homepage endpoints accessible without authentication.
/// </summary>
[AllowAnonymous]
public class HomeController : BaseRestController
{
    private readonly ICommonHomeService _commonHomeService;

    /// <summary>
    /// Initializes a new <see cref="HomeController"/> with its required service.
    /// </summary>
    /// <param name="commonHomeService">The service providing the public homepage overview.</param>
    public HomeController(ICommonHomeService commonHomeService)
    {
        _commonHomeService = commonHomeService;
    }

    /// <summary>
    /// Returns the public animal overview, newest first, optionally filtered by city.
    /// Anonymous and regular users see only public animals; moderators and admins see all.
    /// </summary>
    /// <param name="filterByOrt">Optional case-insensitive city name to filter by.</param>
    /// <param name="paging">Paging parameters (page and page size, 20 per page by default).</param>
    /// <returns>A paged response with the total count and the page of overview items.</returns>
    [HttpGet("animals")]
    public async Task<ActionResult<GetPublicSpecimenOverviewResponseDto>> GetPublicSpecimenOverviewAsync(
        [FromQuery] string? filterByOrt,
        [FromQuery] BaseSortAndPagingDto paging)
    {
        return Ok(await _commonHomeService.GetPublicSpecimenOverviewAsync(
            OptionalAuthentication,
            filterByOrt,
            paging));
    }
}
