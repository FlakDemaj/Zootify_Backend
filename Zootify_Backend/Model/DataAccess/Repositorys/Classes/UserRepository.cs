using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Base;
using Zootify_Backend.Enums;
using Zootify_Backend.Extensions;
using Zootify_Backend.Model.DataAccess.DBContext;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Classes;
using Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Classes;

/// <summary>
/// Repository for querying and persisting <see cref="UserEntity"/> records.
/// </summary>
public class UserRepository : BaseRepository<UserEntity>, IUserRepository
{
    /// <summary>
    /// Initializes a new <see cref="UserRepository"/> with the given database context.
    /// </summary>
    /// <param name="context">The database context to use for persistence operations.</param>
    public UserRepository(ZootifyDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Returns the user with the given GUID, eagerly loading audit user navigation properties, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    public Task<UserEntity?> GetUserByGuidWithAuditInformationAsync(Guid guid)
    {
        return Context
            .GetTrackingSet<UserEntity>()
            .Include(u => u.UpdatedByUser)
            .Include(u => u.DeletedByUser)
            .FirstOrDefaultAsync(u => u.Guid == guid);
    }

    /// <summary>
    /// Returns the active user with the given GUID, or <c>null</c> if not found or the user is not active.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    public async Task<UserEntity?> GetActiveUserByGuid(Guid guid)
    {
        return await Context.GetTrackingSet<UserEntity>()
            .Include(u => u.Settings)
            .FirstOrDefaultAsync(u => u.Guid == guid
                                      && u.Status == UserStatus.Active);
    }

    /// <summary>
    /// Returns the soft-deleted user with the given GUID, or <c>null</c> if not found or the user is not deleted.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    public async Task<UserEntity?> GetDeletedUserByGuid(Guid guid)
    {
        return await Context.GetTrackingSet<UserEntity>()
            .FirstOrDefaultAsync(u => u.Guid == guid
                                      && u.Status == UserStatus.Deleted);
    }

    /// <summary>
    /// Returns the user with the given GUID if their status is active or blocked, or <c>null</c> otherwise.
    /// Used by management operations that may edit, block, or reactivate a user.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    public async Task<UserEntity?> GetActiveOrBlockedUserByGuid(Guid guid)
    {
        return await Context.GetTrackingSet<UserEntity>()
            .FirstOrDefaultAsync(u => u.Guid == guid
                                      && (u.Status == UserStatus.Active
                                          || u.Status == UserStatus.Blocked));
    }

    /// <summary>
    /// Returns a filtered and sorted page of users matching the criteria in the given filter DTO.
    /// </summary>
    /// <param name="query">The filter, sort, and paging parameters.</param>
    /// <returns>The matching <see cref="UserEntity"/> records for the requested page.</returns>
    public async Task<List<UserEntity>> GetAllUsersAsync(
        GetAllUsersFilterDto query)
    {
        var userQuery = Context
            .GetTrackingSet<UserEntity>()
            .OrderBy(u => u.Guid)
            .AsQueryable();

        var filteredQuery = FilterUserQuery(userQuery, query);

        var sortedQuery = SortUserQuery(
            filteredQuery,
            query.SortBy,
            query.SortDirection);

        return await sortedQuery
            .Skip((query.Page-1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Returns all active admin and moderator users, eagerly loading their settings.
    /// </summary>
    /// <returns>All active <see cref="UserEntity"/> records with admin or moderator roles.</returns>
    public async Task<List<UserEntity>> GetAllAdminsAndModeratorUsersWithSettingsAsync()
    {
        return await Context
            .GetTrackingSet<UserEntity>()
            .Include(u => u.Settings)
            .Where(u => u.Status == UserStatus.Active
                && (u.Role == UserRole.Moderator || u.Role == UserRole.Admin))
            .ToListAsync();
    }

    /// <summary>
    /// Returns all users whose GUIDs are in the given list.
    /// </summary>
    /// <param name="guids">The list of user GUIDs to retrieve.</param>
    /// <returns>The matching <see cref="UserEntity"/> records.</returns>
    public async Task<List<UserEntity>> GetUsersByGuidsAsync(List<Guid> guids)
    {
        return await Context.GetTrackingSet<UserEntity>()
            .Include(u => u.Settings)
            .Where(u => guids.Contains(u.Guid))
            .ToListAsync();
    }

    /// <summary>
    /// Returns a batch of soft-deleted users whose deletion date is older than the given cutoff, for purging.
    /// </summary>
    /// <param name="cutoff">The exclusive upper bound for the deletion date.</param>
    /// <param name="batchSize">The maximum number of users to return.</param>
    /// <returns>The matching soft-deleted <see cref="UserEntity"/> records ordered by deletion date.</returns>
    public async Task<List<UserEntity>> GetDeletedUsersForPurgeAsync(DateTime cutoff, int batchSize)
    {
        return await Context.GetTrackingSet<UserEntity>()
            .Where(u => u.Status == UserStatus.Deleted
                && u.DeletionDate != null
                && u.DeletionDate < cutoff)
            .OrderBy(u => u.DeletionDate)
            .Take(batchSize)
            .ToListAsync();
    }

    /// <summary>
    /// Detaches all foreign-key references to the given users so the users can be hard-deleted without
    /// violating the <c>ON DELETE NO ACTION</c> constraints: audit columns (created/updated/deleted/
    /// approved-by) on specimens, collections and taxonomy nodes are set to <c>null</c>, and loans in
    /// which a purged user is the lender or borrower are deleted.
    /// </summary>
    /// <param name="userGuids">The GUIDs of the users about to be purged.</param>
    public async Task PurgeUserReferencesAsync(List<Guid> userGuids)
    {
        // Specimens: null the three audit columns referencing the purged users.
        await Context.Set<SpecimenEntity>()
            .Where(s => s.CreatedByUserGuid != null && userGuids.Contains(s.CreatedByUserGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.CreatedByUserGuid, (Guid?)null));
        await Context.Set<SpecimenEntity>()
            .Where(s => s.UpdatedByUserGuid != null && userGuids.Contains(s.UpdatedByUserGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.UpdatedByUserGuid, (Guid?)null));
        await Context.Set<SpecimenEntity>()
            .Where(s => s.DeletedByUserGuid != null && userGuids.Contains(s.DeletedByUserGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.DeletedByUserGuid, (Guid?)null));

        // Collections: same three audit columns.
        await Context.Set<CollectionEntity>()
            .Where(c => c.CreatedByUserGuid != null && userGuids.Contains(c.CreatedByUserGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.CreatedByUserGuid, (Guid?)null));
        await Context.Set<CollectionEntity>()
            .Where(c => c.UpdatedByUserGuid != null && userGuids.Contains(c.UpdatedByUserGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.UpdatedByUserGuid, (Guid?)null));
        await Context.Set<CollectionEntity>()
            .Where(c => c.DeletedByUserGuid != null && userGuids.Contains(c.DeletedByUserGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.DeletedByUserGuid, (Guid?)null));

        // Taxonomy nodes: only created-by and approved-by reference users.
        await Context.Set<TaxonomyNodeEntity>()
            .Where(t => t.CreatedByUserGuid != null && userGuids.Contains(t.CreatedByUserGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.CreatedByUserGuid, (Guid?)null));
        await Context.Set<TaxonomyNodeEntity>()
            .Where(t => t.ApprovedByUserGuid != null && userGuids.Contains(t.ApprovedByUserGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.ApprovedByUserGuid, (Guid?)null));

        // Loans referencing a purged user as lender or borrower are removed. (The borrower is not merely
        // nulled because LoanEntity models BorrowerGuid as non-nullable, so a null borrower_guid could no
        // longer be materialized by EF and would break the loan-reminder/return flows.)
        await Context.Set<LoanEntity>()
            .Where(l => userGuids.Contains(l.LenderGuid) || userGuids.Contains(l.BorrowerGuid))
            .ExecuteDeleteAsync();

        // Loan requests made by a purged user are removed (requester_guid is non-nullable). The nullable
        // approver/rejecter references are cleared so a purged approver no longer blocks the hard-delete.
        await Context.Set<LoanRequestEntity>()
            .Where(r => userGuids.Contains(r.RequesterGuid))
            .ExecuteDeleteAsync();
        await Context.Set<LoanRequestEntity>()
            .Where(r => r.AdminApproverGuid != null && userGuids.Contains(r.AdminApproverGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.AdminApproverGuid, (Guid?)null));
        await Context.Set<LoanRequestEntity>()
            .Where(r => r.CreatorApproverGuid != null && userGuids.Contains(r.CreatorApproverGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.CreatorApproverGuid, (Guid?)null));
        await Context.Set<LoanRequestEntity>()
            .Where(r => r.RejectedByGuid != null && userGuids.Contains(r.RejectedByGuid.Value))
            .ExecuteUpdateAsync(set => set.SetProperty(r => r.RejectedByGuid, (Guid?)null));
    }

    /// <summary>
    /// Applies filter criteria from the given DTO to the user queryable.
    /// </summary>
    /// <param name="userQuery">The base queryable to filter.</param>
    /// <param name="query">The filter parameters to apply.</param>
    /// <returns>The filtered queryable.</returns>
    private static IQueryable<UserEntity> FilterUserQuery(
        IQueryable<UserEntity> userQuery,
        GetAllUsersFilterDto query)
    {
        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            userQuery = userQuery.Where(q => q.Email.Contains(query.Email));
        }
        if (!string.IsNullOrWhiteSpace(query.FirstName))
        {
            userQuery = userQuery.Where(q => q.FirstName.Contains(query.FirstName));
        }
        if (!string.IsNullOrWhiteSpace(query.LastName))
        {
            userQuery = userQuery.Where(q => q.LastName.Contains(query.LastName));
        }
        if (query.Role != null)
        {
            userQuery = userQuery.Where(q => q.Role == query.Role);
        }
        if (query.Status != null)
        {
            userQuery = userQuery.Where(q => q.Status == query.Status);
        }

        return userQuery;
    }

    /// <summary>
    /// Applies ordering to the user queryable based on the given sort field and direction.
    /// </summary>
    /// <param name="userQuery">The queryable to sort.</param>
    /// <param name="sortBy">The name of the field to sort by.</param>
    /// <param name="sortDirection">The sort direction.</param>
    /// <returns>The ordered queryable.</returns>
    private static IQueryable<UserEntity> SortUserQuery(
        IQueryable<UserEntity> userQuery,
        string? sortBy,
        SortDirection sortDirection)
    {
        switch (sortBy)
        {
            case "firstName":
                return ApplySort(userQuery, u => u.FirstName, sortDirection);
            case "lastName":
                return ApplySort(userQuery, u => u.LastName, sortDirection);
            case "email":
                return ApplySort(userQuery, u => u.Email, sortDirection);
            case "role":
                return ApplySort(userQuery, u => u.Role, sortDirection);
            case "status":
                return ApplySort(userQuery, u => u.Status, sortDirection);
            default:
                return ApplySort(userQuery, u => u.Guid, sortDirection);
        }
    }

    /// <summary>
    /// Orders the given user queryable by the specified key selector and direction, with GUID as the tie-breaker.
    /// </summary>
    /// <typeparam name="TKey">The type of the sort key.</typeparam>
    /// <param name="userQuery">The queryable to sort.</param>
    /// <param name="keySelector">The expression selecting the sort key.</param>
    /// <param name="direction">The sort direction.</param>
    /// <returns>The ordered queryable.</returns>
    private static IOrderedQueryable<UserEntity> ApplySort<TKey>(
        IQueryable<UserEntity> userQuery,
        Expression<Func<UserEntity, TKey>> keySelector,
        SortDirection direction)
    {
        if (direction == SortDirection.Desc)
        {
            return userQuery.OrderByDescending(keySelector).ThenBy(q => q.Guid);
        }
        
        return userQuery.OrderBy(keySelector).ThenBy(q => q.Guid);
    }
}