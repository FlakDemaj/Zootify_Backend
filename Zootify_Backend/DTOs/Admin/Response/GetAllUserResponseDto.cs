using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Admin.Response;

/// <summary>
/// A single user entry returned in the admin user list.
/// </summary>
public class GetAllUserResponseDto
{
    /// <summary>The user's GUID.</summary>
    public required Guid Guid { get; init; }

    /// <summary>The user's first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>The user's last name.</summary>
    public required string LastName { get; init; }

    /// <summary>The user's email address.</summary>
    public required string Email { get; init; }

    /// <summary>The user's role.</summary>
    public UserRole Role { get; init; }

    /// <summary>The user's account status.</summary>
    public UserStatus Status { get; init; }
}
