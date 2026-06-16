namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for updating only the user's preferred language.
/// </summary>
public class UpdateUserSettingsLanguageRequestDto
{
    /// <summary>The new language code (e.g. "de" or "en").</summary>
    public required string Language { get; init; }
}
