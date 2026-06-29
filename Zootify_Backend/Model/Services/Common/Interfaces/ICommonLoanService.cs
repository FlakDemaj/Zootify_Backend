using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines loan read and borrower operations available to all authenticated roles: listing active loans,
/// viewing a single loan, listing the caller's borrowed loans, and returning them themselves.
/// </summary>
public interface ICommonLoanService
{
    /// <summary>
    /// Returns a paged list of active (not yet returned) loans. Admins and moderators see all active loans;
    /// regular users only see loans of specimens they created.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A paged response containing the active loans and the total count.</returns>
    Task<GetActiveLoansPagedResponseDto> GetActiveLoansAsync(IAuthentication authentication, BaseSortAndPagingDto paging);

    /// <summary>
    /// Returns the full detail view of the active loan with the given GUID. Admins and moderators may view
    /// any active loan; regular users may only view loans of specimens they created.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="loanGuid">The GUID of the loan to retrieve.</param>
    /// <returns>The detail DTO of the active loan.</returns>
    /// <exception cref="ZootifyException">Thrown when no active loan with the given GUID is visible to the caller.</exception>
    Task<LoanDetailDto> GetActiveLoanByGuidAsync(IAuthentication authentication, Guid loanGuid);

    /// <summary>
    /// Returns the active (not yet returned) loans the caller has borrowed.
    /// </summary>
    /// <param name="authentication">The authentication context of the borrowing user.</param>
    Task<List<LoanOverviewDto>> GetBorrowedLoansAsync(IAuthentication authentication);

    /// <summary>
    /// Marks a loan the caller has borrowed as returned.
    /// </summary>
    /// <param name="authentication">The authentication context of the borrowing user.</param>
    /// <param name="loanGuid">The GUID of the loan to return.</param>
    /// <exception cref="ZootifyException">Thrown when the caller has no such active borrowed loan.</exception>
    Task ReturnOwnLoanAsync(IAuthentication authentication, Guid loanGuid);
}
