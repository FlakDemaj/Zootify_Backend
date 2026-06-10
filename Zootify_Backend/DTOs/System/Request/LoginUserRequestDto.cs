namespace Zootify_Backend.DTOs.System.Request;

public class LoginUserRequestDto
{
    public required string Email { get; set; }
    
    public required string Password { get; set; }
}