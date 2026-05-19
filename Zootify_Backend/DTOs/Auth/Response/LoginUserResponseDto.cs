namespace Zootify_Backend.DTOs.Auth.Response;

public class LoginUserResponseDto
{
    public required string Token { get; set; }
    
    public required int ExpiresInMinutes { get; set; }
}