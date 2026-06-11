using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

public interface ILoanRepository : IBaseRepository<LoanEntity>
{
    Task<LoanEntity?> GetLoanByGuidWithDetailsAsync(Guid guid);
    
    Task<List<LoanEntity>> GetLoansForStage1ReminderAsync(DateTime dueBefore, int batchSize);

    Task<List<LoanEntity>> GetLoansForStage2ReminderAsync(DateTime dueBefore, int batchSize);

    Task<List<LoanEntity>> GetLoansForStage3ReminderAsync(DateTime dueBefore, int batchSize);
}
