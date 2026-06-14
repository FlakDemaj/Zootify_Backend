namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateUserSettingsRequestDto
{
    public required bool DarkMode { get; init; }

    public required bool SendPushNotification { get; init; }

    public required bool SendEmail { get; init; }
    
    public required string Language { get; init; }
}
