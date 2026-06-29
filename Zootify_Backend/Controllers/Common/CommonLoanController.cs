using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zootify_Backend.Controllers.Base;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Services.Common.Interfaces;

namespace Zootify_Backend.Controllers.Common;

/// <summary>
/// Exposes loan read and borrower endpoints to all authenticated roles: listing and viewing active loans
/// (admins/moderators see all, regular users only loans of specimens they created), listing the loans the
/// caller currently has borrowed, and returning them themselves.
/// </summary>
[Authorize(Roles = $"{Roles.Admin},{Roles.Moderator},{Roles.User}")]
public class CommonLoanController : BaseRestController
{
    private readonly ICommonLoanService _commonLoanService;

    /// <summary>
    /// Initializes a new <see cref="CommonLoanController"/> with its required service.
    /// </summary>
    /// <param name="commonLoanService">The service used to manage loan reads and the caller's borrowed loans.</param>
    public CommonLoanController(ICommonLoanService commonLoanService)
    {
        _commonLoanService = commonLoanService;
    }

    /// <summary>
    /// Returns a paged list of active (not yet returned) loans. Admins and moderators see all active loans;
    /// regular users only see loans of specimens they created.
    /// </summary>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A paged response containing the active loans and the total count.</returns>
    [HttpGet("active")]
    public async Task<ActionResult<GetActiveLoansPagedResponseDto>> GetActiveLoansAsync(
        [FromQuery] BaseSortAndPagingDto paging)
    {
        return Ok(await _commonLoanService.GetActiveLoansAsync(Authentication, paging));
    }

    /// <summary>
    /// Returns the full detail view of a single active (not yet returned) loan. Admins and moderators may view
    /// any active loan; regular users may only view loans of specimens they created.
    /// </summary>
    /// <param name="loanGuid">The GUID of the loan to retrieve.</param>
    /// <returns>The detail DTO of the active loan.</returns>
    [HttpGet("{loanGuid:guid}")]
    public async Task<ActionResult<LoanDetailDto>> GetActiveLoanAsync(Guid loanGuid)
    {
        return Ok(await _commonLoanService.GetActiveLoanByGuidAsync(Authentication, loanGuid));
    }

    /// <summary>
    /// Returns the active (not yet returned) loans the caller has borrowed.
    /// </summary>
    /// <returns>A list of the caller's active borrowed loans.</returns>
    [HttpGet("borrowed")]
    public async Task<ActionResult<List<LoanOverviewDto>>> GetBorrowedAsync()
    {
        return Ok(await _commonLoanService.GetBorrowedLoansAsync(Authentication));
    }

    /// <summary>
    /// Marks a loan the caller has borrowed as returned.
    /// </summary>
    /// <param name="loanGuid">The GUID of the loan to return.</param>
    [HttpPost("{loanGuid:guid}/return")]
    public async Task<ActionResult> ReturnAsync(Guid loanGuid)
    {
        await _commonLoanService.ReturnOwnLoanAsync(Authentication, loanGuid);
        return Ok();
    }
}
