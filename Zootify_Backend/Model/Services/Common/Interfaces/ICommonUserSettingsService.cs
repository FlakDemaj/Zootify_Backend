using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines operations for managing a user's personal application settings.
/// </summary>
public interface ICommonUserSettingsService
{
    /// <summary>
    /// Updates the authenticated user's personal settings such as dark mode and language preference.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="requestDto">The new settings values to apply.</param>
    Task UpdateUserSettingsAsync(
        IAuthentication authentication,
        UpdateUserSettingsRequestDto requestDto);
}
