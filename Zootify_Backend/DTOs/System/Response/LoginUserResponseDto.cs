using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.DTOs.System.Response;

/// <summary>
/// Response returned after a successful login, carrying the auth token and basic profile data.
/// </summary>
public class LoginUserResponseDto
{
    /// <summary>The issued authentication token.</summary>
    public required TokenResult Token { get; init; }

    /// <summary>The user's first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>The user's last name.</summary>
    public required string LastName { get; init; }

    /// <summary>The user's display-relevant settings (language, dark mode).</summary>
    public required UserSettingsDto  UserSettings { get; init; }
}
