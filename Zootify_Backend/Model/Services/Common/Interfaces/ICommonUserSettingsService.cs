using Zootify_Backend.DTOs.Common.Request;
using Zootify_Backend.DTOs.Common.Response;
using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.Model.Services.Common.Interfaces;

/// <summary>
/// Defines operations for managing a user's personal application settings.
/// </summary>
public interface ICommonUserSettingsService
{
    /// <summary>
    /// Returns the authenticated user's personal settings.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <returns>The user's personal settings.</returns>
    /// <exception cref="ZootifyException">Thrown when the user has no settings.</exception>
    Task<GetUserSettingsResponseDto> GetUserSettingsAsync(IAuthentication authentication);

    /// <summary>
    /// Updates the authenticated user's personal settings such as dark mode and language preference.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="requestDto">The new settings values to apply.</param>
    /// <exception cref="ZootifyException">Thrown when the user has no settings.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    Task UpdateUserSettingsAsync(
        IAuthentication authentication,
        UpdateUserSettingsRequestDto requestDto);

    /// <summary>
    /// Updates the authenticated user's personal settings language.
    /// </summary>
    /// <param name="authentication">The authentication context of the requesting user.</param>
    /// <param name="requestDto">The new language values to apply.</param>
    /// <exception cref="ZootifyException">Thrown when the user has no settings.</exception>
    /// <exception cref="ZootifyException">Thrown when no changes are detected.</exception>
    Task UpdateUserSettingsLanguageAsync(IAuthentication authentication, UpdateUserSettingsLanguageRequestDto requestDto);
}
