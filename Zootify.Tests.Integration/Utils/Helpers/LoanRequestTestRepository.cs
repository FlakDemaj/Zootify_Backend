using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Model.DataAccess.DBContext;

namespace Zootify.Tests.Integration.Utils.Helpers;

/// <summary>
/// Loan Request Test Repository — test support type.
/// </summary>
public class LoanRequestTestRepository
{
    private readonly ZootifyDbContext _dbContext;

    public LoanRequestTestRepository(ZootifyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Test support method: Create Loan Request Async.
    /// </summary>
    public async Task<LoanRequestEntity> CreateLoanRequestAsync(
        Guid specimenGuid,
        Guid requesterGuid,
        DateTime? desiredDueDate = null,
        LoanRequestStatus status = LoanRequestStatus.Pending,
        string? note = null,
        Guid? adminApproverGuid = null,
        DateTime? adminApprovedAt = null,
        Guid? creatorApproverGuid = null,
        DateTime? creatorApprovedAt = null)
    {
        var request = new LoanRequestEntity
        {
            SpecimenGuid        = specimenGuid,
            RequesterGuid       = requesterGuid,
            Status              = status,
            DesiredDueDate      = desiredDueDate ?? DateTime.UtcNow.AddDays(14),
            RequesterNote       = note,
            RequestedAt         = DateTime.UtcNow,
            AdminApproverGuid   = adminApproverGuid,
            AdminApprovedAt     = adminApprovedAt,
            CreatorApproverGuid = creatorApproverGuid,
            CreatorApprovedAt   = creatorApprovedAt,
        };

        _dbContext.Add(request);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();
        return request;
    }

    /// <summary>
    /// Test support method: Get Loan Request From Db Async.
    /// </summary>
    public async Task<LoanRequestEntity?> GetByGuidAsync(Guid guid)
    {
        return await _dbContext.Set<LoanRequestEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Guid == guid);
    }
}
