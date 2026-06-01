namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateUserSettingsRequestDto
{
    public required bool DarkMode { get; set; }

    public required string Language { get; set; }
}
