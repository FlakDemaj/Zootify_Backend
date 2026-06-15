using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Controllers.Admin;

/// <summary>
/// Exposes taxonomy approval and rejection endpoints restricted to Admin and Moderator roles.
/// </summary>
[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
public class AdminTaxonomyController : BaseRestController
{
    private readonly IAdminTaxonomyService _adminTaxonomyService;

    /// <summary>
    /// Initializes a new <see cref="AdminTaxonomyController"/> with its required service.
    /// </summary>
    /// <param name="adminTaxonomyService">The service used to manage taxonomy approval operations.</param>
    public AdminTaxonomyController(IAdminTaxonomyService adminTaxonomyService)
    {
        _adminTaxonomyService = adminTaxonomyService;
    }

    /// <summary>
    /// Returns the taxonomy tree filtered to only include subtrees that contain at least one pending node.
    /// </summary>
    /// <returns>A taxonomy tree response DTO containing all pending subtrees.</returns>
    [HttpGet("for-approval")]
    public async Task<ActionResult<GetTaxonomyTreeResponseDto>> GetTaxonomiesForApprovalAsync()
    {
        return Ok(await _adminTaxonomyService.GetTaxonomiesForApprovalAsync());
    }

    /// <summary>
    /// Returns existing approved taxonomies of the same rank whose name sounds similar to the node
    /// under review, as a decision aid when approving or rejecting it.
    /// </summary>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node being reviewed.</param>
    /// <returns>The reviewed node together with its most similar existing approved taxonomies.</returns>
    [HttpGet("{taxonomyGuid:guid}/similar")]
    public async Task<ActionResult<GetSimilarTaxonomiesResponseDto>> GetSimilarTaxonomiesAsync(Guid taxonomyGuid)
    {
        return Ok(await _adminTaxonomyService.GetSimilarTaxonomiesAsync(taxonomyGuid));
    }

    /// <summary>
    /// Approves a pending taxonomy node and activates any specimens whose full taxonomy chain is now approved.
    /// </summary>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node to approve.</param>
    [HttpPost("{taxonomyGuid:guid}/approve")]
    public async Task<IActionResult> ApproveTaxonomyAsync(Guid taxonomyGuid)
    {
        await _adminTaxonomyService.ApproveTaxonomyAsync(Authentication, taxonomyGuid);
        return Ok();
    }

    /// <summary>
    /// Rejects a taxonomy node, hard-deletes its entire subtree, and rejects all associated specimens.
    /// </summary>
    /// <param name="taxonomyGuid">The GUID of the taxonomy node to reject.</param>
    /// <param name="request">Contains the rejection comment to send to affected users.</param>
    [HttpPost("{taxonomyGuid:guid}/reject")]
    public async Task<IActionResult> RejectTaxonomyAsync(
        Guid taxonomyGuid,
        [FromBody] RejectTaxonomyRequestDto request)
    {
        await _adminTaxonomyService.RejectTaxonomyAsync(Authentication, taxonomyGuid, request);
        return Ok();
    }
}
