namespace Zootify_Backend.DTOs.System.Request;

public class RegisterUserRequestDto
{
    public required string FirstName { get; init; }
    
    public required string LastName { get; init; }
    
    public required string Email { get; init; }
    
    public required string Password { get; init; }
}
