using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with loan-specific query operations including reminder stage queries.
/// </summary>
public interface ILoanRepository : IBaseRepository<LoanEntity>
{
    /// <summary>
    /// Returns a paged list of active (not yet returned) loans, eagerly loading borrower and specimen details.
    /// Admins and moderators see all active loans; regular users only see loans of specimens they created.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user, used for visibility filtering.</param>
    /// <param name="paging">Paging and sorting parameters.</param>
    /// <returns>A tuple containing the page of active loans and the total matching count.</returns>
    Task<(List<LoanEntity> Items, int TotalCount)> GetActiveLoansPagedAsync(
        IAuthentication authentication,
        BaseSortAndPagingDto paging);

    /// <summary>
    /// Returns the active (not yet returned) loan with the given GUID, eagerly loading borrower, lender,
    /// and specimen (including its image) details, or <c>null</c> if no matching active loan is visible to the caller.
    /// Admins and moderators may see any active loan; regular users only see loans of specimens they created.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user, used for visibility filtering.</param>
    /// <param name="guid">The GUID of the loan to retrieve.</param>
    Task<LoanEntity?> GetActiveLoanByGuidWithDetailsAsync(IAuthentication authentication, Guid guid);

    /// <summary>
    /// Returns whether the given specimen currently has an active (not yet returned) loan.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen to check.</param>
    Task<bool> HasActiveLoanForSpecimenAsync(Guid specimenGuid);

    /// <summary>
    /// Returns the active (not yet returned) loans the given user has borrowed, eagerly loading specimen
    /// (including its image) and borrower details, ordered by due date.
    /// </summary>
    /// <param name="borrowerGuid">The GUID of the borrowing user.</param>
    Task<List<LoanEntity>> GetActiveBorrowedLoansAsync(Guid borrowerGuid);

    /// <summary>
    /// Returns the active (not yet returned) loan with the given GUID borrowed by the given user, eagerly
    /// loading borrower (including their settings) and specimen details, or <c>null</c> if no such loan
    /// exists. Used to scope a borrower's self-return to their own loans and to notify them of the return.
    /// </summary>
    /// <param name="borrowerGuid">The GUID of the borrowing user.</param>
    /// <param name="loanGuid">The GUID of the loan to retrieve.</param>
    Task<LoanEntity?> GetActiveBorrowedLoanByGuidAsync(Guid borrowerGuid, Guid loanGuid);

    /// <summary>
    /// Returns a batch of unreturned loans whose due date falls within the stage-1 band and that have
    /// not yet had a stage-1 reminder sent. The band is half-open <c>[dueFrom, dueBefore)</c> so the
    /// reminder stages are mutually exclusive and a loan triggers at most one stage per run.
    /// </summary>
    /// <param name="dueFrom">The inclusive lower bound for the due date (the stage-2 cutoff).</param>
    /// <param name="dueBefore">The exclusive upper bound for the due date.</param>
    /// <param name="batchSize">The maximum number of loans to return.</param>
    Task<List<LoanEntity>> GetLoansForStage1ReminderAsync(DateTime dueFrom, DateTime dueBefore, int batchSize);

    /// <summary>
    /// Returns a batch of unreturned loans whose due date falls within the stage-2 band and that have
    /// not yet had a stage-2 reminder sent. The band is half-open <c>[dueFrom, dueBefore)</c>.
    /// </summary>
    /// <param name="dueFrom">The inclusive lower bound for the due date (the stage-3 cutoff).</param>
    /// <param name="dueBefore">The exclusive upper bound for the due date.</param>
    /// <param name="batchSize">The maximum number of loans to return.</param>
    Task<List<LoanEntity>> GetLoansForStage2ReminderAsync(DateTime dueFrom, DateTime dueBefore, int batchSize);

    /// <summary>
    /// Returns a batch of unreturned loans due before the given date that have not yet had a stage-3 reminder sent.
    /// </summary>
    /// <param name="dueBefore">The exclusive upper bound for the due date.</param>
    /// <param name="batchSize">The maximum number of loans to return.</param>
    Task<List<LoanEntity>> GetLoansForStage3ReminderAsync(DateTime dueBefore, int batchSize);
}
