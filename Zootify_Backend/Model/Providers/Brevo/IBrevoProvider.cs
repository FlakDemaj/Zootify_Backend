using Zootify_Backend.Enums;

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

    public Task<bool> SendTaxonomyApprovedEmail(
        string email,
        string firstname,
        string lastname,
        string taxonomyName,
        TaxonomyRank rank);

    public Task<bool> SendTaxonomyRejectedEmail(
        string email,
        string firstname,
        string lastname,
        string taxonomyName,
        TaxonomyRank rank,
        string comment);

    public Task<bool> SendLoanConfirmationEmail(
        string borrowerEmail,
        string borrowerName,
        string lenderName,
        string specimenName,
        string specimenLatinName,
        DateTime loanDate,
        DateTime dueDate,
        string? notes);

    public Task<bool> SendLoanReturnConfirmationEmail(
        string borrowerEmail,
        string borrowerName,
        string specimenName,
        string specimenLatinName,
        DateTime loanDate,
        DateTime returnDate,
        string? notes);

    public Task<bool> SendLoanDueReminderEmail(
        string recipientEmail,
        string recipientName,
        string specimenName,
        string specimenLatinName,
        int daysUntilDue,
        DateTime dueDate,
        bool isBorrower);
}
