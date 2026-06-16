using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with loan-specific query operations including reminder stage queries.
/// </summary>
public interface ILoanRepository : IBaseRepository<LoanEntity>
{
    /// <summary>
    /// Returns the loan with the given GUID, eagerly loading borrower and specimen details, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the loan to retrieve.</param>
    Task<LoanEntity?> GetLoanByGuidWithDetailsAsync(Guid guid);

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
