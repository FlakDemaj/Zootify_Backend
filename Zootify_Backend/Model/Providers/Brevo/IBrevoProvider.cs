namespace Zootify_Backend.Model.Providers.Brevo;

/// <summary>
/// Defines transactional email operations sent via Brevo.
/// </summary>
public interface IBrevoProvider
{
    /// <summary>
    /// Sends a registration confirmation email containing the activation link.
    /// </summary>
    /// <param name="token">The registration token embedded in the confirmation link.</param>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public Task<bool> SendRegistrationEmail(
        string token,
        string email,
        string firstname,
        string lastname);

    /// <summary>
    /// Sends a confirmation email notifying the user that their password was successfully reset.
    /// </summary>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <param name="email">The recipient's email address.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public Task<bool> SendConfirmationResetPasswordEmail(
        string firstname,
        string lastname,
        string email);

    /// <summary>
    /// Sends a password-reset email containing the reset link.
    /// </summary>
    /// <param name="resetToken">The reset token embedded in the reset link.</param>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public Task<bool> SendResetPasswordEmail(
        string resetToken,
        string email,
        string firstname,
        string lastname);
}
