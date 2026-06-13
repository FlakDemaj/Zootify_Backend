using Zootify_Backend.DTOs.System.Request;
using Zootify_Backend.DTOs.System.Response;

namespace Zootify_Backend.Model.Services.System.Interfaces;

/// <summary>
/// Defines all user-related business operations including registration, login, and password management.
/// </summary>
public interface ISystemAuthService
{
    /// <summary>
    /// Authenticates a user with the provided credentials and returns a JWT token.
    /// </summary>
    /// <param name="request">The login credentials including email and password.</param>
    /// <returns>A DTO containing the user's name, JWT token, and settings.</returns>
    public Task<LoginUserResponseDto> LoginAsync(LoginUserRequestDto request);

    /// <summary>
    /// Registers a new user, creates a registration token, and sends a confirmation email.
    /// </summary>
    /// <param name="request">The registration data including email, name, and password.</param>
    public Task RegisterAsync(RegisterUserRequestDto request);

    /// <summary>
    /// Confirms a user's registration using the token sent by email and activates the account.
    /// </summary>
    /// <param name="token">The registration token sent to the user by email.</param>
    public Task ConfirmRegistrationAsync(string token);

    /// <summary>
    /// Resends the registration confirmation email to a pending user with a fresh token.
    /// </summary>
    /// <param name="request">Contains the email address of the pending user.</param>
    public Task SendNewRegistrationEmailAsync(SendNewRegistrationEmailRequestDto request);

    /// <summary>
    /// Sends a password-reset email to an active user with a fresh reset token.
    /// </summary>
    /// <param name="request">Contains the email address of the user requesting a password reset.</param>
    public Task SendNewResetPasswordEmailAsync(SendNewResetPasswordEmailRequestDto request);

    /// <summary>
    /// Resets the user's password using a valid reset token and sends a confirmation email.
    /// </summary>
    /// <param name="request">The new password data including the password and its confirmation.</param>
    /// <param name="resetPasswordToken">The reset token sent to the user by email.</param>
    public Task ResetPasswordAsync(ResetPasswordRequestDto request, string resetPasswordToken);
}
