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

    /// <summary>
    /// Sends an email notifying the user that a taxonomy node they created has been approved.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <param name="taxonomyName">The name of the approved taxonomy node.</param>
    /// <param name="rank">The rank of the approved taxonomy node.</param>
    /// <param name="language">The recipient's preferred language code (e.g. "de" or "en"); German is used when null or unrecognized.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public Task<bool> SendTaxonomyApprovedEmail(
        string email,
        string firstname,
        string lastname,
        string taxonomyName,
        TaxonomyRank rank,
        string? language);

    /// <summary>
    /// Sends an email notifying the user that a taxonomy node they created has been rejected.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <param name="taxonomyName">The name of the rejected taxonomy node.</param>
    /// <param name="rank">The rank of the rejected taxonomy node.</param>
    /// <param name="comment">The rejection comment provided by the moderator or admin.</param>
    /// <param name="language">The recipient's preferred language code (e.g. "de" or "en"); German is used when null or unrecognized.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public Task<bool> SendTaxonomyRejectedEmail(
        string email,
        string firstname,
        string lastname,
        string taxonomyName,
        TaxonomyRank rank,
        string comment,
        string? language);

    /// <summary>
    /// Sends a loan confirmation email to the borrower with all loan details.
    /// </summary>
    /// <param name="borrowerEmail">The borrower's email address.</param>
    /// <param name="borrowerName">The borrower's full name.</param>
    /// <param name="lenderName">The lender's full name.</param>
    /// <param name="specimenName">The common name of the loaned specimen.</param>
    /// <param name="specimenLatinName">The latin name of the loaned specimen.</param>
    /// <param name="loanDate">The date the loan was created.</param>
    /// <param name="dueDate">The date by which the specimen must be returned.</param>
    /// <param name="notes">Optional notes attached to the loan.</param>
    /// <param name="language">The borrower's preferred language code (e.g. "de" or "en"); German is used when null or unrecognized.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public Task<bool> SendLoanConfirmationEmail(
        string borrowerEmail,
        string borrowerName,
        string lenderName,
        string specimenName,
        string specimenLatinName,
        DateTime loanDate,
        DateTime dueDate,
        string? notes,
        string? language);

    /// <summary>
    /// Sends a return confirmation email to the borrower confirming the specimen has been returned.
    /// </summary>
    /// <param name="borrowerEmail">The borrower's email address.</param>
    /// <param name="borrowerName">The borrower's full name.</param>
    /// <param name="specimenName">The common name of the returned specimen.</param>
    /// <param name="specimenLatinName">The latin name of the returned specimen.</param>
    /// <param name="loanDate">The original loan date.</param>
    /// <param name="returnDate">The date the specimen was returned.</param>
    /// <param name="notes">Optional notes attached to the loan.</param>
    /// <param name="language">The borrower's preferred language code (e.g. "de" or "en"); German is used when null or unrecognized.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public Task<bool> SendLoanReturnConfirmationEmail(
        string borrowerEmail,
        string borrowerName,
        string specimenName,
        string specimenLatinName,
        DateTime loanDate,
        DateTime returnDate,
        string? notes,
        string? language);

    /// <summary>
    /// Sends a due-date reminder email to either the borrower or lender of a loan.
    /// </summary>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="recipientName">The recipient's full name.</param>
    /// <param name="specimenName">The common name of the loaned specimen.</param>
    /// <param name="specimenLatinName">The latin name of the loaned specimen.</param>
    /// <param name="daysUntilDue">The number of days remaining until the loan is due (negative if overdue).</param>
    /// <param name="dueDate">The due date of the loan.</param>
    /// <param name="isBorrower">Whether the recipient is the borrower (<c>true</c>) or the lender (<c>false</c>).</param>
    /// <param name="language">The recipient's preferred language code (e.g. "de" or "en"); German is used when null or unrecognized.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public Task<bool> SendLoanDueReminderEmail(
        string recipientEmail,
        string recipientName,
        string specimenName,
        string specimenLatinName,
        int daysUntilDue,
        DateTime dueDate,
        bool isBorrower,
        string? language);
}
