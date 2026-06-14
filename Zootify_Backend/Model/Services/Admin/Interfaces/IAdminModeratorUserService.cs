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
    /// Updates the data of an existing user. Only Admins may perform this action and cannot update themselves.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting admin.</param>
    /// <param name="userToUpdateGuid">The GUID of the user to update.</param>
    /// <param name="updateUserRequestDto">The new data to apply to the user.</param>
    /// <exception cref="ZootifyException">Thrown when the caller does not have permission.</exception>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    Task UpdateUserAsync(
        IAuthentication authentication,
        Guid userToUpdateGuid,
        UpdateUserRequestDto updateUserRequestDto);

    /// <summary>
    /// Re-activates a previously deleted user account.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting admin.</param>
    /// <param name="userToActivateGuid">The GUID of the deleted user to activate.</param>
    /// <exception cref="ZootifyException">Thrown when no user with the given GUID exists.</exception>
    Task ActivateUserAsync(
        IAuthentication authentication,
        Guid userToActivateGuid);
}
