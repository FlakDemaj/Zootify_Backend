using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="LoanEntity"/> records.
/// </summary>
public class LoanRepository : BaseRepository<LoanEntity>, ILoanRepository
{
    /// <summary>
    /// Initializes a new <see cref="LoanRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public LoanRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns the loan with the given GUID, eagerly loading borrower and specimen details, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the loan to retrieve.</param>
    public async Task<LoanEntity?> GetLoanByGuidWithDetailsAsync(Guid guid)
    {
        return await Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower).ThenInclude(b => b.Settings)
            .Include(l => l.Specimen)
            .FirstOrDefaultAsync(l => l.Guid == guid);
    }
    
    /// <summary>
    /// Returns a batch of unreturned loans due before the given date that have not yet had a stage-1 reminder sent.
    /// </summary>
    /// <param name="dueBefore">The exclusive upper bound for the due date.</param>
    /// <param name="batchSize">The maximum number of loans to return.</param>
    public async Task<List<LoanEntity>> GetLoansForStage1ReminderAsync(DateTime dueFrom, DateTime dueBefore, int batchSize)
    {
        return await ReminderQuery()
            .Where(l => l.Stage1ReminderSentAt == null
                        && l.DueDate >= dueFrom
                        && l.DueDate < dueBefore)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Returns a batch of unreturned loans due before the given date that have not yet had a stage-2 reminder sent.
    /// </summary>
    /// <param name="dueBefore">The exclusive upper bound for the due date.</param>
    /// <param name="batchSize">The maximum number of loans to return.</param>
    public async Task<List<LoanEntity>> GetLoansForStage2ReminderAsync(DateTime dueFrom, DateTime dueBefore, int batchSize)
    {
        return await ReminderQuery()
            .Where(l => l.Stage2ReminderSentAt == null
                        && l.DueDate >= dueFrom
                        && l.DueDate < dueBefore)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Returns a batch of unreturned loans due before the given date that have not yet had a stage-3 reminder sent.
    /// </summary>
    /// <param name="dueBefore">The exclusive upper bound for the due date.</param>
    /// <param name="batchSize">The maximum number of loans to return.</param>
    public async Task<List<LoanEntity>> GetLoansForStage3ReminderAsync(DateTime dueBefore, int batchSize)
    {
        return await ReminderQuery()
            .Where(l => l.Stage3ReminderSentAt == null && l.DueDate < dueBefore)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Builds the base query for unreturned loans with borrower, lender (including their settings),
    /// and specimen eagerly loaded, ordered by due date. The settings are needed so each reminder
    /// can be sent in the recipient's preferred language.
    /// </summary>
    /// <returns>The base <see cref="IQueryable{LoanEntity}"/> for reminder queries.</returns>
    private IQueryable<LoanEntity> ReminderQuery()
    {
        return Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower).ThenInclude(b => b.Settings)
            .Include(l => l.Lender).ThenInclude(l => l.Settings)
            .Include(l => l.Specimen)
            .Where(l => l.ReturnDate == null)
            .OrderBy(l => l.DueDate);
    }
}
