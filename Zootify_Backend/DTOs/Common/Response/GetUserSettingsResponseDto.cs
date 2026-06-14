namespace Zootify_Backend.DTOs.Common.Response;

public class GetUserSettingsResponseDto
{
    public required bool DarkMode { get; init; }

    public required bool SendPushNotification { get; init; }

    public required bool SendEmail { get; init; }

    public required string Language { get; init; }
}
