namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateOwnUserRequestDto
{
    public required string FirstName { get; init; }
    
    public required string LastName { get; init; }
    
    public required string Email { get; init; }
}