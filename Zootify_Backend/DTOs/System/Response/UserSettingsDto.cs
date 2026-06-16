namespace Zootify_Backend.DTOs.System.Response;

/// <summary>
/// The minimal user settings returned alongside a login response.
/// </summary>
public class UserSettingsDto
{
    /// <summary>The preferred language code (e.g. "de" or "en").</summary>
    public required string Language { get; init; }

    /// <summary>Whether dark mode is enabled.</summary>
    public required bool DarkMode { get; init; }
}
