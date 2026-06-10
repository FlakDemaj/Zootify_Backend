namespace Zootify_Backend.DTOs.System.Response;

public class UserSettingsDto
{
    public required string Language { get; set; }
    
    public required bool DarkMode { get; set; }
}