using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetUserResponseDto
{
    public required Guid UserGuid { get; set; }
    
    public required string FirstName { get; set; }

    public required string LastName { get; set; }
    
    public required string Email { get; set; }
    
    public UserStatus UserStatus { get; set; }
    
    public UserRole UserRole { get; set; }
    
    public required int LoginAttempts { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    public string? UpdatedBy { get; set; }
    
    public DateTime? DeletedAt { get; set; }
    
    public string? DeletedBy { get; set; }
}