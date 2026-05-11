namespace Zootify_Backend.DTOs.Response;

public class LoginUserResponseDto
{
    public required string Token { get; set; }
    
    public required int ExpiresInMinutes { get; set; }
}