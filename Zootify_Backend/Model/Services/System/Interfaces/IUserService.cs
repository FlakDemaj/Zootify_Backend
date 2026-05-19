using Zootify_Backend.DTOs.Auth.Request;
using Zootify_Backend.DTOs.Auth.Response;

namespace Zootify_Backend.Model.Services.System.Interfaces;

/// <summary>
/// Defines all user-related business operations including registration, login, and password management.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Authenticates a user with the provided credentials and returns a JWT token.
    /// </summary>
    public Task<LoginUserResponseDto> LoginAsync(LoginUserRequestDto request);

    /// <summary>
    /// Registers a new user, creates a registration token, and sends a confirmation email.
    /// </summary>
    public Task RegisterAsync(RegisterUserRequestDto request);

    /// <summary>
    /// Confirms a user's registration using the token sent by email and activates the account.
    /// </summary>
    public Task ConfirmRegistrationAsync(string token);

    /// <summary>
    /// Resends the registration confirmation email to a pending user with a fresh token.
    /// </summary>
    public Task SendNewRegistrationEmailAsync(SendNewRegistrationEmailRequestDto request);

    /// <summary>
    /// Sends a password-reset email to an active user with a fresh reset token.
    /// </summary>
    public Task SendNewResetPasswordEmailAsync(SendNewResetPasswordEmailRequestDto request);

    /// <summary>
    /// Resets the user's password using a valid reset token and sends a confirmation email.
    /// </summary>
    public Task ResetPasswordAsync(ResetPasswordRequestDto request, string resetPasswordToken);
}
