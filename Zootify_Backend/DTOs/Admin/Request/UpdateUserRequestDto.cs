using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Admin.Request;

/// <summary>
/// Request payload for an admin/moderator updating another user's core data.
/// </summary>
public class UpdateUserRequestDto
{
    /// <summary>The user's new first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>The user's new last name.</summary>
    public required string LastName { get; init; }

    /// <summary>The user's new role.</summary>
    public required UserRole Role { get; init; }
}
