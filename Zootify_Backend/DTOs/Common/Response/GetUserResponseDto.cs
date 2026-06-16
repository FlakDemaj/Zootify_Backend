using Zootify_Backend.Enums;

namespace Zootify_Backend.DTOs.Common.Response;

/// <summary>
/// The detail view of a single user, including audit metadata.
/// </summary>
public class GetUserResponseDto
{
    /// <summary>The user's GUID.</summary>
    public required Guid UserGuid { get; init; }

    /// <summary>The user's first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>The user's last name.</summary>
    public required string LastName { get; init; }

    /// <summary>The user's email address.</summary>
    public required string Email { get; init; }

    /// <summary>The user's account status.</summary>
    public UserStatus UserStatus { get; init; }

    /// <summary>The user's role.</summary>
    public UserRole UserRole { get; init; }

    /// <summary>The current number of consecutive failed login attempts.</summary>
    public required int LoginAttempts { get; init; }

    /// <summary>When the user was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>When the user was last updated.</summary>
    public DateTime? UpdatedAt { get; init; }

    /// <summary>The display name of the user who last updated this user.</summary>
    public string? UpdatedBy { get; init; }

    /// <summary>When the user was deleted.</summary>
    public DateTime? DeletedAt { get; init; }

    /// <summary>The display name of the user who deleted this user.</summary>
    public string? DeletedBy { get; init; }
}
