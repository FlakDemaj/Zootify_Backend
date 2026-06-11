using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Admin.Response;

public class GetAllUserResponseDto
{
    public required Guid Guid { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public UserRole Role { get; init; }
    public UserStatus Status { get; init; }
}