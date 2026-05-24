using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Admin.Request;

public class UpdateUserRequestDto
{
    public required string FirstName { get; set; }
    
    public required string LastName { get; set; }
    
    public required UserRole Role { get; set; }
    
}