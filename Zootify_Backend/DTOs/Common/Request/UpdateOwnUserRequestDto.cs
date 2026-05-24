namespace Zootify_Backend.DTOs.Common.Request;

public class UpdateOwnUserRequestDto
{
    public required string FirstName { get; set; }
    
    public required string LastName { get; set; }
    
    public required string Email { get; set; }
}