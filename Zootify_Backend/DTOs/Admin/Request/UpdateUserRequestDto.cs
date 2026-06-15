using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Admin.Request;

public class UpdateUserRequestDto
{
    public required string FirstName { get; init; }
    
    public required string LastName { get; init; }
    
    public required UserRole Role { get; init; }
    
    public UserStatus Status { get; init; }
    
}