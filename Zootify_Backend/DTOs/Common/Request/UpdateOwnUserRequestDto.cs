namespace Zootify_Backend.DTOs.Common.Request;

/// <summary>
/// Request payload for a user updating their own profile data.
/// </summary>
public class UpdateOwnUserRequestDto
{
    /// <summary>The user's new first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>The user's new last name.</summary>
    public required string LastName { get; init; }

    /// <summary>The user's new email address.</summary>
    public required string Email { get; init; }
}
