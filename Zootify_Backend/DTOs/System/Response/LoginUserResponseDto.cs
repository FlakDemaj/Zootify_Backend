using Zootify_Backend.Model.Auth;

namespace Zootify_Backend.DTOs.System.Response;

public class LoginUserResponseDto
{
    public required TokenResult Token { get; init; }
    
    public required string FirstName { get; init; }
    
    public required string LastName { get; init; }
    
    public required UserSettingsDto  UserSettings { get; init; }

}