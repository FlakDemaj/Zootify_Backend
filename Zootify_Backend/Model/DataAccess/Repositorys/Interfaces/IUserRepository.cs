using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.Model.DataAccess.Entitys;
using Zootify_Backend.Model.DataAccess.Repositorys.Base.Interfaces;

namespace Zootify_Backend.Model.DataAccess.Repositorys.Interfaces;

/// <summary>
/// Extends <see cref="IBaseRepository{T}"/> with user-specific query operations.
/// </summary>
public interface IUserRepository : IBaseRepository<UserEntity>
{
    /// <summary>
    /// Returns the user with the given GUID, eagerly loading audit user navigation properties, or <c>null</c> if not found.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    Task<UserEntity?> GetUserByGuidWithAuditInformationAsync(Guid guid);

    /// <summary>
    /// Returns an active user with the given GUID, or <c>null</c> if not found or not active.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    Task<UserEntity?> GetActiveUserByGuid(Guid guid);

    /// <summary>
    /// Returns a deleted user with the given GUID, or <c>null</c> if not found or not deleted.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    Task<UserEntity?> GetDeletedUserByGuid(Guid guid);

    /// <summary>
    /// Returns an active or blocked user with the given GUID, or <c>null</c> if not found, pending, or deleted.
    /// Used by management operations that may edit, block, or reactivate a user.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    Task<UserEntity?> GetActiveOrBlockedUserByGuid(Guid guid);

    /// <summary>
    /// Returns a filtered and paged list of all users in the system.
    /// </summary>
    /// <param name="query">Filter, sorting, and paging parameters.</param>
    Task<List<UserEntity>> GetAllUsersAsync(
        GetAllUsersFilterDto query);

    /// <summary>
    /// Returns all active admin and moderator users with their settings eagerly loaded, used for notifications.
    /// </summary>
    Task<List<UserEntity>> GetAllAdminsAndModeratorUsersWithSettingsAsync();

    /// <summary>
    /// Returns all users whose GUIDs are in the given list.
    /// </summary>
    /// <param name="guids">The list of user GUIDs to look up.</param>
    Task<List<UserEntity>> GetUsersByGuidsAsync(List<Guid> guids);

    /// <summary>
    /// Returns a batch of soft-deleted users whose deletion date is older than the given cutoff.
    /// </summary>
    /// <param name="cutoff">The maximum allowed deletion date.</param>
    /// <param name="batchSize">The maximum number of users to return.</param>
    Task<List<UserEntity>> GetDeletedUsersForPurgeAsync(DateTime cutoff, int batchSize);

    /// <summary>
    /// Clears all foreign-key references to the given users (audit columns on specimens, collections and
    /// taxonomy nodes; loan lender/borrower) so the users can subsequently be hard-deleted without
    /// violating the database's <c>ON DELETE NO ACTION</c> constraints.
    /// </summary>
    /// <param name="userGuids">The GUIDs of the users about to be purged.</param>
    Task PurgeUserReferencesAsync(List<Guid> userGuids);
}