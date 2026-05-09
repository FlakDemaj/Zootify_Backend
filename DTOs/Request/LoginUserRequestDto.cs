namespace Zootify_Backend.DTOs.Request;

public class LoginUserRequestDto
{
    public required string Email { get; set; }
    
    public required string Password { get; set; }
}