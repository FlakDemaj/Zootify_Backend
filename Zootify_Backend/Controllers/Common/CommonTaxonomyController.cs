using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes taxonomy node management endpoints accessible to all authenticated roles.
/// </summary>
[Authorize(Roles = $"{Roles.Moderator}, {Roles.Admin}, {Roles.User}")]
public class CommonTaxonomyController : BaseRestController
{
    private readonly ICommonTaxonomyNodeService _commonTaxonomyNodeService;

    /// <summary>
    /// Initializes a new <see cref="CommonTaxonomyController"/> with its required service.
    /// </summary>
    /// <param name="commonTaxonomyNodeService">The service used to manage taxonomy node operations.</param>
    public CommonTaxonomyController(ICommonTaxonomyNodeService commonTaxonomyNodeService)
    {
        _commonTaxonomyNodeService = commonTaxonomyNodeService;
    }

    /// <summary>
    /// Returns all taxonomy nodes of the specified rank that match the given name fragment, each accompanied by their ancestor path.
    /// </summary>
    /// <param name="filter">Optional name fragment to filter nodes by.</param>
    /// <param name="rank">The taxonomy rank to filter by.</param>
    /// <returns>A list of taxonomy chains, where each chain starts from a root node down to the matching node.</returns>
    [HttpGet]
    public async Task<ActionResult<List<List<GetAllTaxonomyNodesByFilterDto>>>> GetTaxonomyNodesForCreation(
        [FromQuery] string? filter)
    {
        return Ok(await _commonTaxonomyNodeService.GetAllTaxonomyNodesAsync(
            filter));
    }

    /// <summary>
    /// Returns the full taxonomy tree, optionally filtered by a root-node name fragment.
    /// </summary>
    /// <param name="filterByName">Optional name fragment to filter root nodes by.</param>
    /// <returns>A hierarchical taxonomy tree response DTO.</returns>
    [HttpGet("tree")]
    public async Task<ActionResult<GetTaxonomyTreeResponseDto>> GetAllTaxonomyNodesAsTreeAsync(
        [FromQuery] string? filterByName)
    {
        return Ok(await _commonTaxonomyNodeService.GetAllTaxonomyNodesAsTreeAsync(filterByName));
    }

    /// <summary>
    /// Creates one or more new taxonomy nodes in a chain, optionally rooted under an existing parent node.
    /// </summary>
    /// <param name="requestDto">The taxonomy node chain to create, including an optional parent GUID.</param>
    /// <returns>A list of DTOs containing the GUIDs of the created nodes.</returns>
    [HttpPost]
    public async Task<ActionResult<List<CreatedObjectResponseDto>>> CreateTaxonomyNode(
        [FromBody] CreateTaxonomyNodeRequestDto requestDto)
    {
        return Ok(await _commonTaxonomyNodeService.CreateTaxonomyNodeAsync(Authentication, requestDto));
    }

}