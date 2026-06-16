namespace Zootify_Backend.DTOs.System.Request;

/// <summary>
/// Request payload for logging a user in.
/// </summary>
public class LoginUserRequestDto
{
    /// <summary>The user's email address.</summary>
    public required string Email { get; init; }

    /// <summary>The user's plain-text password.</summary>
    public required string Password { get; init; }
}
