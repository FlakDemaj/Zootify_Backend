using AutoMapper;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Model.Providers.Brevo;
using Zootify_Backend.Model.Services.Common.Interfaces;
using Zootify_Backend.Transaction;

namespace Zootify_Backend.Model.Services.Common.Classes;

/// <summary>
/// Handles a borrower's own loans: listing active borrowed loans and self-returning them.
/// </summary>
public class CommonLoanService : ICommonLoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBrevoProvider _brevoProvider;
    private readonly IPostCommitQueue _postCommitQueue;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="CommonLoanService"/> with its required dependencies.
    /// </summary>
    /// <param name="loanRepository">Repository for loan persistence operations.</param>
    /// <param name="brevoProvider">Email provider for sending the return confirmation email.</param>
    /// <param name="postCommitQueue">Queue used to dispatch the confirmation email only after the transaction commits.</param>
    /// <param name="mapper">AutoMapper instance for object mapping.</param>
    public CommonLoanService(
        ILoanRepository loanRepository,
        IBrevoProvider brevoProvider,
        IPostCommitQueue postCommitQueue,
        IMapper mapper)
    {
        _loanRepository = loanRepository;
        _brevoProvider = brevoProvider;
        _postCommitQueue = postCommitQueue;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns a paged list of active (not yet returned) loans. Admins and moderators see all active loans;
    /// regular users only see loans of specimens they created.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A paged response containing the active loans and the total count.</returns>
    [Transaction(ReadOnly = true)]
    public async Task<GetActiveLoansPagedResponseDto> GetActiveLoansAsync(
        IAuthentication authentication,
        BaseSortAndPagingDto paging)
    {
        var (items, total) = await _loanRepository.GetActiveLoansPagedAsync(authentication, paging);

        return new GetActiveLoansPagedResponseDto
        {
            TotalCount = total,
            Loans      = _mapper.Map<List<LoanOverviewDto>>(items),
        };
    }

    /// <summary>
    /// Returns the full detail view of the active loan with the given GUID. Admins and moderators may view
    /// any active loan; regular users may only view loans of specimens they created.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="loanGuid">The GUID of the loan to retrieve.</param>
    /// <returns>The detail DTO of the active loan.</returns>
    /// <exception cref="ZootifyException">Thrown when no active loan with the given GUID is visible to the caller.</exception>
    [Transaction(ReadOnly = true)]
    public async Task<LoanDetailDto> GetActiveLoanByGuidAsync(IAuthentication authentication, Guid loanGuid)
    {
        var loan = await _loanRepository.GetActiveLoanByGuidWithDetailsAsync(authentication, loanGuid);
        if (loan == null)
        {
            throw new ZootifyException(ErrorCode.LoanNotFound);
        }

        return _mapper.Map<LoanDetailDto>(loan);
    }

    /// <summary>
    /// Returns the active (not yet returned) loans the caller has borrowed.
    /// </summary>
    /// <param name="authentication">The authentication context of the borrowing user.</param>
    [Transaction(ReadOnly = true)]
    public async Task<List<LoanOverviewDto>> GetBorrowedLoansAsync(IAuthentication authentication)
    {
        var loans = await _loanRepository.GetActiveBorrowedLoansAsync(authentication.UserGuid);

        return _mapper.Map<List<LoanOverviewDto>>(loans);
    }

    /// <summary>
    /// Marks a loan the caller has borrowed as returned and sends them a return confirmation email
    /// (when enabled in their settings).
    /// </summary>
    /// <param name="authentication">The authentication context of the borrowing user.</param>
    /// <param name="loanGuid">The GUID of the loan to return.</param>
    /// <exception cref="ZootifyException">Thrown when the caller has no such active borrowed loan.</exception>
    [Transaction(ReadOnly = false)]
    public async Task ReturnOwnLoanAsync(IAuthentication authentication, Guid loanGuid)
    {
        var loan = await _loanRepository.GetActiveBorrowedLoanByGuidAsync(authentication.UserGuid, loanGuid);
        if (loan == null)
        {
            throw new ZootifyException(ErrorCode.LoanNotFound);
        }

        loan.ReturnDate = DateTime.UtcNow;

        await _loanRepository.UpdateAsync(loan);

        var borrower = loan.Borrower;
        var borrowerName = $"{borrower.FirstName} {borrower.LastName}".Trim();
        var returnDate = loan.ReturnDate.Value;

        // The borrower is emailed a return confirmation in their preferred language, only when they have
        // email notifications enabled and only after the transaction commits so they are never notified
        // about a return that was rolled back.
        _postCommitQueue.Enqueue(async () =>
        {
            if (borrower.Settings.SendEmail)
            {
                await _brevoProvider.SendLoanReturnConfirmationEmail(
                    borrower.Email,
                    borrowerName,
                    loan.Specimen.Name,
                    loan.Specimen.LatinName,
                    loan.LoanDate,
                    returnDate,
                    loan.Notes,
                    borrower.Settings.Language);
            }
        });
    }
}
