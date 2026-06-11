using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Controllers.Admin;

[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
public class AdminLoanController : BaseRestController
{
    private readonly IAdminLoanService _adminLoanService;

    public AdminLoanController(IAdminLoanService adminLoanService)
    {
        _adminLoanService = adminLoanService;
    }

    [HttpGet("borrowers")]
    public async Task<ActionResult<List<BorrowerPickerDto>>> GetBorrowersAsync(
        [FromQuery] string startsWith)
    {
        return Ok(await _adminLoanService.GetBorrowersAsync(startsWith));
    }

    [HttpPost]
    public async Task<ActionResult<CreatedObjectResponseDto>> CreateLoanAsync(
        [FromBody] CreateLoanRequestDto request)
    {
        return Ok(await _adminLoanService.CreateLoanAsync(Authentication, request));
    }

    [HttpPost("{loanGuid:guid}/return")]
    public async Task<ActionResult> ReturnLoanAsync(
        Guid loanGuid,
        [FromBody] ReturnLoanRequestDto request)
    {
        await _adminLoanService.ReturnLoanAsync(loanGuid, request);
        return Ok();
    }
}
