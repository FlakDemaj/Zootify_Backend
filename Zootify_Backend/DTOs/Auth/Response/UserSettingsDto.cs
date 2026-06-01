namespace Zootify_Backend.DTOs.Auth.Response;

public class UserSettingsDto
{
    public required string Language { get; set; }
    
    public required bool DarkMode { get; set; }
}