namespace Zootify_Backend.DTOs.Auth.Request;

public class LoginUserRequestDto
{
    public required string Email { get; set; }
    
    public required string Password { get; set; }
}