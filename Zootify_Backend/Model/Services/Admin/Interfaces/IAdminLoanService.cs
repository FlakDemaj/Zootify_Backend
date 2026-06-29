using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Admin.Interfaces;

/// <summary>
/// Defines operations for managing specimen loan records.
/// </summary>
public interface IAdminLoanService
{
    /// <summary>
    /// Returns a list of active users whose names contain the given fragment, for use in borrower picker controls.
    /// </summary>
    /// <param name="filter">The name fragment to filter user names by.</param>
    /// <returns>A list of matching borrower picker DTOs.</returns>
    Task<List<BorrowerPickerDto>> GetBorrowersAsync(string filter);

    /// <summary>
    /// Returns a paged list of active (not yet returned) loans.
    /// </summary>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A paged response containing the active loans and the total count.</returns>
    Task<GetActiveLoansPagedResponseDto> GetActiveLoansAsync(BaseSortAndPagingDto paging);

    /// <summary>
    /// Returns the full detail view of the active loan with the given GUID.
    /// </summary>
    /// <param name="loanGuid">The GUID of the loan to retrieve.</param>
    /// <returns>The detail DTO of the active loan.</returns>
    /// <exception cref="ZootifyException">Thrown when no active loan with the given GUID is found.</exception>
    Task<LoanDetailDto> GetActiveLoanByGuidAsync(Guid loanGuid);

    /// <summary>
    /// Creates a new loan record and sends a confirmation email to the borrower.
    /// </summary>
    /// <param name="authentication">The authentication context of the lending user.</param>
    /// <param name="request">The loan creation data including specimen, borrower, due date, and notes.</param>
    /// <returns>A DTO containing the GUID of the newly created loan.</returns>
    /// <exception cref="ZootifyException">Thrown when no specimen with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    Task<CreatedObjectResponseDto> CreateLoanAsync(IAuthentication authentication, CreateLoanRequestDto request);

    /// <summary>
    /// Marks a loan as returned, appends optional notes, and sends a return confirmation email to the borrower.
    /// </summary>
    /// <param name="loanGuid">The GUID of the loan to mark as returned.</param>
    /// <param name="request">Optional notes to append to the loan record.</param>
    /// <exception cref="ZootifyException">Thrown when no loan with the given GUID is found.</exception>
    /// <exception cref="ZootifyException">Thrown when the loan has already been returned.</exception>
    Task ReturnLoanAsync(Guid loanGuid, ReturnLoanRequestDto request);
}
