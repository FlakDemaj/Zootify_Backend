namespace Zootify_Backend.DTOs.System.Request;

/// <summary>
/// Request payload for registering a new user.
/// </summary>
public class RegisterUserRequestDto
{
    /// <summary>The user's first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>The user's last name.</summary>
    public required string LastName { get; init; }

    /// <summary>The user's email address.</summary>
    public required string Email { get; init; }

    /// <summary>The user's chosen plain-text password.</summary>
    public required string Password { get; init; }
}
