namespace Zootify.Tests.Integration.Utils.Helpers;

public class LoanTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public LoanTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LoanEntity> CreateLoanAsync(
        Guid specimenGuid,
        Guid lenderGuid,
        Guid borrowerGuid,
        DateTime dueDate,
        DateTime? loanDate = null,
        DateTime? returnDate = null,
        DateTime? stage1SentAt = null,
        DateTime? stage2SentAt = null,
        DateTime? stage3SentAt = null,
        string? notes = null)
    {
        var loan = new LoanEntity
        {
            SpecimenGuid          = specimenGuid,
            LenderGuid            = lenderGuid,
            BorrowerGuid          = borrowerGuid,
            LoanDate              = loanDate ?? DateTime.UtcNow,
            DueDate               = dueDate,
            ReturnDate            = returnDate,
            Stage1ReminderSentAt  = stage1SentAt,
            Stage2ReminderSentAt  = stage2SentAt,
            Stage3ReminderSentAt  = stage3SentAt,
            Notes                 = notes,
        };

        _dbContext.Add(loan);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        return loan;
    }
}
