namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// The user's current settings.
/// </summary>
public class GetUserSettingsResponseDto
{
    /// <summary>Whether dark mode is enabled.</summary>
    public required bool DarkMode { get; init; }

    /// <summary>Whether push notifications are enabled.</summary>
    public required bool SendPushNotification { get; init; }

    /// <summary>Whether email notifications are enabled.</summary>
    public required bool SendEmail { get; init; }

    /// <summary>The preferred language code (e.g. "de" or "en").</summary>
    public required string Language { get; init; }
}
