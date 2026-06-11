using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Controllers.Admin;

[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
public class AdminTaxonomyController : BaseRestController
{
    private readonly IAdminTaxonomyService _adminTaxonomyService;

    public AdminTaxonomyController(IAdminTaxonomyService adminTaxonomyService)
    {
        _adminTaxonomyService = adminTaxonomyService;
    }

    [HttpGet("for-approval")]
    public async Task<ActionResult<GetTaxonomyTreeResponseDto>> GetTaxonomiesForApprovalAsync()
    {
        return Ok(await _adminTaxonomyService.GetTaxonomiesForApprovalAsync());
    }

    [HttpPost("{taxonomyGuid:guid}/approve")]
    public async Task<IActionResult> ApproveTaxonomyAsync(Guid taxonomyGuid)
    {
        await _adminTaxonomyService.ApproveTaxonomyAsync(Authentication, taxonomyGuid);
        return Ok();
    }

    [HttpPost("{taxonomyGuid:guid}/reject")]
    public async Task<IActionResult> RejectTaxonomyAsync(
        Guid taxonomyGuid,
        [FromBody] RejectTaxonomyRequestDto request)
    {
        await _adminTaxonomyService.RejectTaxonomyAsync(Authentication, taxonomyGuid, request);
        return Ok();
    }
}
