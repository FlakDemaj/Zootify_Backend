using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Anonym;

[AllowAnonymous]
public class AnonymCollectionController : BaseRestController
{
    private readonly ICommonCollectionService _commonCollectionService;

    public AnonymCollectionController(ICommonCollectionService commonCollectionService)
    {
        _commonCollectionService = commonCollectionService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<GetCollectionResponseDto>>> GetPublicCollectionsAsync()
    {
        return Ok(await _commonCollectionService.GetPublicCollectionsAsync(null));
    }
}