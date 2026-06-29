using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Admin.Interfaces;

namespace Zootify_Backend.Controllers.AdminAndModerator;

/// <summary>
/// Exposes loan management endpoints restricted to Admin and Moderator roles.
/// </summary>
[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator}")]
public class AdminLoanController : BaseRestController
{
    private readonly IAdminLoanService _adminLoanService;

    /// <summary>
    /// Initializes a new <see cref="AdminLoanController"/> with its required service.
    /// </summary>
    /// <param name="adminLoanService">The service used to manage loan operations.</param>
    public AdminLoanController(IAdminLoanService adminLoanService)
    {
        _adminLoanService = adminLoanService;
    }

    /// <summary>
    /// Returns a list of active users whose names contain the given fragment, for use in borrower picker controls.
    /// </summary>
    /// <param name="filter">The name fragment to filter user names by.</param>
    /// <returns>A list of matching borrower picker DTOs.</returns>
    [HttpGet("borrowers")]
    public async Task<ActionResult<List<BorrowerPickerDto>>> GetBorrowersAsync(
        [FromQuery] string filter)
    {
        return Ok(await _adminLoanService.GetBorrowersAsync(filter));
    }

    /// <summary>
    /// Returns a paged list of all active (not yet returned) loans.
    /// </summary>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A paged response containing the active loans and the total count.</returns>
    [HttpGet("active")]
    public async Task<ActionResult<GetActiveLoansPagedResponseDto>> GetActiveLoansAsync(
        [FromQuery] BaseSortAndPagingDto paging)
    {
        return Ok(await _adminLoanService.GetActiveLoansAsync(paging));
    }

    /// <summary>
    /// Returns the full detail view of a single active (not yet returned) loan.
    /// </summary>
    /// <param name="loanGuid">The GUID of the loan to retrieve.</param>
    /// <returns>The detail DTO of the active loan.</returns>
    [HttpGet("{loanGuid:guid}")]
    public async Task<ActionResult<LoanDetailDto>> GetActiveLoanAsync(Guid loanGuid)
    {
        return Ok(await _adminLoanService.GetActiveLoanByGuidAsync(loanGuid));
    }

    /// <summary>
    /// Creates a new loan record and sends a confirmation email to the borrower.
    /// </summary>
    /// <param name="request">The details of the loan to create.</param>
    /// <returns>A DTO containing the GUID of the newly created loan.</returns>
    [HttpPost]
    public async Task<ActionResult<CreatedObjectResponseDto>> CreateLoanAsync(
        [FromBody] CreateLoanRequestDto request)
    {
        return Ok(await _adminLoanService.CreateLoanAsync(Authentication, request));
    }

    /// <summary>
    /// Marks an existing loan as returned and sends a return confirmation email to the borrower.
    /// </summary>
    /// <param name="loanGuid">The GUID of the loan to return.</param>
    /// <param name="request">Optional appended notes to add to the loan record.</param>
    [HttpPost("{loanGuid:guid}/return")]
    public async Task<ActionResult> ReturnLoanAsync(
        Guid loanGuid,
        [FromBody] ReturnLoanRequestDto request)
    {
        await _adminLoanService.ReturnLoanAsync(loanGuid, request);
        return Ok();
    }
}
