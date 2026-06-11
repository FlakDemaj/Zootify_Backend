using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;
using Zootify_Backend.Models.DataAccess.DBContext;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

public class LoanRepository : BaseRepository<LoanEntity>, ILoanRepository
{
    public LoanRepository(ZootifyDbContext context) : base(context)
    {
    }

    public async Task<LoanEntity?> GetLoanByGuidWithDetailsAsync(Guid guid)
    {
        return await Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower)
            .Include(l => l.Specimen)
            .FirstOrDefaultAsync(l => l.Guid == guid);
    }

    public async Task<List<LoanEntity>> GetActiveLoansWithDetailsAsync()
    {
        return await Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower)
            .Include(l => l.Lender)
            .Include(l => l.Specimen)
            .Where(l => l.ReturnDate == null)
            .ToListAsync();
    }

    public async Task<List<LoanEntity>> GetLoansForStage1ReminderAsync(DateTime dueBefore, int batchSize)
    {
        return await ReminderQuery()
            .Where(l => l.Stage1ReminderSentAt == null && l.DueDate < dueBefore)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<List<LoanEntity>> GetLoansForStage2ReminderAsync(DateTime dueBefore, int batchSize)
    {
        return await ReminderQuery()
            .Where(l => l.Stage2ReminderSentAt == null && l.DueDate < dueBefore)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<List<LoanEntity>> GetLoansForStage3ReminderAsync(DateTime dueBefore, int batchSize)
    {
        return await ReminderQuery()
            .Where(l => l.Stage3ReminderSentAt == null && l.DueDate < dueBefore)
            .Take(batchSize)
            .ToListAsync();
    }

    private IQueryable<LoanEntity> ReminderQuery()
    {
        return Context.GetTrackingSet<LoanEntity>()
            .Include(l => l.Borrower)
            .Include(l => l.Lender)
            .Include(l => l.Specimen)
            .Where(l => l.ReturnDate == null)
            .OrderBy(l => l.DueDate);
    }
}
