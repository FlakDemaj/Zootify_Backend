using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

public class GetUserResponseDto
{
    public required Guid UserGuid { get; init; }
    
    public required string FirstName { get; init; }

    public required string LastName { get; init; }
    
    public required string Email { get; init; }
    
    public UserStatus UserStatus { get; init; }
    
    public UserRole UserRole { get; init; }
    
    public required int LoginAttempts { get; init; }
    
    public DateTime CreatedAt { get; init; }
    
    public DateTime? UpdatedAt { get; init; }
    
    public string? UpdatedBy { get; init; }
    
    public DateTime? DeletedAt { get; init; }
    
    public string? DeletedBy { get; init; }
}