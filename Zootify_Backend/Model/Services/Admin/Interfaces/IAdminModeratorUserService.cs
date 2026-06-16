using Zootify_Backend.DTOs.Admin.Request;
using Zootify_Backend.DTOs.Admin.Response;
using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Admin.Interfaces;

/// <summary>
/// Defines user management operations available to Admin and Moderator roles.
/// </summary>
public interface IAdminModeratorUserService
{
    /// <summary>
    /// Returns detailed information for a single user identified by their GUID.
    /// </summary>
    /// <param name="guid">The GUID of the user to retrieve.</param>
    /// <returns>A detailed user response DTO.</returns>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    Task<GetUserResponseDto> GetUserByGuidAsync(Guid guid);

    /// <summary>
    /// Returns a filtered and paged list of all users in the system.
    /// </summary>
    /// <param name="query">Filter, sorting, and paging parameters.</param>
    /// <returns>A list of user response DTOs matching the filter criteria.</returns>
    Task<List<GetAllUserResponseDto>> GetAllUsersAsync(GetAllUsersFilterDto query);

    /// <summary>
    /// Updates the data of an existing (active) user. A moderator may only edit regular users; an admin
    /// may edit any user. No caller may update themselves (self-edit goes through the own-profile route).
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting admin or moderator.</param>
    /// <param name="userToUpdateGuid">The GUID of the user to update.</param>
    /// <param name="updateUserRequestDto">The new data to apply to the user.</param>
    /// <exception cref="ZootifyException">Thrown when the caller does not have permission.</exception>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when the target user is not active.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    Task UpdateUserAsync(
        IAuthentication authentication,
        Guid userToUpdateGuid,
        UpdateUserRequestDto updateUserRequestDto);

    /// <summary>
    /// Re-activates a user account that is currently blocked or deleted, setting it back to active.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting admin.</param>
    /// <param name="userToActivateGuid">The GUID of the blocked or deleted user to activate.</param>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when the user is already active.</exception>
    Task ActivateUserAsync(
        IAuthentication authentication,
        Guid userToActivateGuid);

    /// <summary>
    /// Blocks an active user account. A user cannot block themselves.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting admin.</param>
    /// <param name="userToBlockGuid">The GUID of the active user to block.</param>
    /// <exception cref="ZootifyException">Thrown when the caller tries to block themselves.</exception>
    /// <exception cref="ZootifyException">Thrown when no active user with the given GUID exists.</exception>
    Task BlockUserAsync(
        IAuthentication authentication,
        Guid userToBlockGuid);
}
