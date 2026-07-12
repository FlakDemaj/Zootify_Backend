using Microsoft.EntityFrameworkCore;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.Auth;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="LoanRequestEntity"/> records.
/// </summary>
public class LoanRequestRepository : BaseRepository<LoanRequestEntity>, ILoanRequestRepository
{
    /// <summary>
    /// Initializes a new <see cref="LoanRequestRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public LoanRequestRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns the loan request with the given GUID, eagerly loading the specimen (including its image)
    /// and the requester (including their settings), or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the loan request to retrieve.</param>
    public async Task<LoanRequestEntity?> GetByGuidWithDetailsAsync(Guid guid)
    {
        return await Context.GetTrackingSet<LoanRequestEntity>()
            .Include(r => r.Specimen)
            .Include(r => r.Requester).ThenInclude(u => u.Settings)
            .FirstOrDefaultAsync(r => r.Guid == guid);
    }

    /// <summary>
    /// Returns whether the given specimen currently has an open (pending) loan request.
    /// </summary>
    /// <param name="specimenGuid">The GUID of the specimen to check.</param>
    public async Task<bool> HasOpenRequestForSpecimenAsync(Guid specimenGuid)
    {
        return await Context.GetTrackingSet<LoanRequestEntity>()
            .AnyAsync(r => r.SpecimenGuid == specimenGuid && r.Status == LoanRequestStatus.Pending);
    }

    /// <summary>
    /// Returns the pending loan requests the given caller still needs to decide on, ordered by request date.
    /// </summary>
    /// <param name="authentication">The authentication context of the caller, used for the role-based filter.</param>
    public async Task<List<LoanRequestEntity>> GetPendingForApproverAsync(IAuthentication authentication)
    {
        var callerGuid = authentication.UserGuid;
        var isAdminOrModerator = authentication.Role is UserRole.Admin or UserRole.Moderator;

        return await Context.GetTrackingSet<LoanRequestEntity>()
            .Include(r => r.Specimen)
            .Include(r => r.Requester)
            .Where(r => r.Status == LoanRequestStatus.Pending && r.RequesterGuid != callerGuid)
            .Where(r => (isAdminOrModerator && r.AdminApprovedAt == null)
                        || (r.Specimen.CreatedByUserGuid == callerGuid && r.CreatorApprovedAt == null))
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();
    }
}
