namespace Zootify_Backend.DTOs.System.Request;

public class LoginUserRequestDto
{
    public required string Email { get; init; }
    
    public required string Password { get; init; }
}