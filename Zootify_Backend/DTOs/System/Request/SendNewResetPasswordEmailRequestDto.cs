namespace Zootify_Backend.DTOs.System.Request;

/// <summary>
/// Request payload for sending a new reset-password email.
/// </summary>
public class SendNewResetPasswordEmailRequestDto
{
    /// <summary>The email address to send the reset-password email to.</summary>
    public required string Email { get; init; }
}
