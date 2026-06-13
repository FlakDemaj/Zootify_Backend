using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines self-service user operations available to all authenticated roles.
/// </summary>
public interface ICommonUserService
{
    /// <summary>
    /// Updates the authenticated user's own profile data.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="updateOwnUserRequestDto">The new profile data to apply.</param>
    Task UpdateOwnUserAsync(
        IAuthentication authentication,
        UpdateOwnUserRequestDto updateOwnUserRequestDto);

    /// <summary>
    /// Soft-deletes a user account. Admins may delete any user; regular users may only delete themselves.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="userToDeleteGuid">The GUID of the user to delete.</param>
    Task DeleteUserAsync(
        IAuthentication authentication,
        Guid userToDeleteGuid);

    /// <summary>
    /// Returns the authenticated user's own profile and settings data.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <returns>A response DTO containing the current user's data.</returns>
    Task<GetOwnUserDataResponse> GetOwnUserDataAsync(IAuthentication authentication);
}