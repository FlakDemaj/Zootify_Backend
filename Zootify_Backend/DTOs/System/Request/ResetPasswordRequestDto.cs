namespace Zootify_Backend.DTOs.System.Request;

/// <summary>
/// Request payload for setting a new password via a reset-password token.
/// </summary>
public class ResetPasswordRequestDto
{
    /// <summary>The new plain-text password.</summary>
    public required string Password { get; init; }

    /// <summary>The repeated password, which must match <see cref="Password"/>.</summary>
    public required string RepeatedPassword { get; init; }
}
