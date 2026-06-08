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
public class CommonTaxonomyController : BaseRestController
{
    private readonly ICommonTaxonomyNodeService _commonTaxonomyNodeService;

    public CommonTaxonomyController(ICommonTaxonomyNodeService commonTaxonomyNodeService)
    {
        _commonTaxonomyNodeService = commonTaxonomyNodeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<List<GetAllTaxonomyNodesByFilterDto>>>> GetTaxonomyNodesForCreation(
        [FromQuery] string? startsWith,
        [FromQuery] TaxonomyRank rank)
    {
        return Ok(await _commonTaxonomyNodeService.GetAllTaxonomyNodesAsync(
            startsWith,
            rank));
    }
    
    [HttpPost]
    public async Task<ActionResult<CreatedObjectResponseDto>> CreateTaxonomyNode(
        [FromBody] CreateTaxonomyNodeRequestDto requestDto)
    {
        return Ok(await _commonTaxonomyNodeService.CreateTaxonomyNodeAsync(Authentication, requestDto));
    }
    
}