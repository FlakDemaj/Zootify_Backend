namespace Zootify_Backend.DTOs.System.Request;

/// <summary>
/// Request payload for resending a registration confirmation email.
/// </summary>
public class SendNewRegistrationEmailRequestDto
{
    /// <summary>The email address to resend the registration email to.</summary>
    public required string Email { get; init; }
}
