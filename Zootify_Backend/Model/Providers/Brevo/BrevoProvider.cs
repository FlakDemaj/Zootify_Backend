using System.Globalization;
using SendWithBrevo;
using Zootify_Backend.Configuration;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Model.Providers.Brevo;

/// <summary>
/// Email provider implementation using Brevo (formerly Sendinblue) for sending
/// transactional emails such as registration confirmations and password resets.
/// </summary>
public class BrevoProvider : IBrevoProvider
{
    private BrevoClient BrevoClient { get; }

    /// <summary>
    /// Initializes a new <see cref="BrevoProvider"/> with the configured Brevo client.
    /// </summary>
    /// <param name="brevoClient">The Brevo client used to send transactional emails.</param>
    public BrevoProvider(BrevoClient brevoClient)
    {
        BrevoClient = brevoClient;
    }

    /// <summary>
    /// Sends a registration confirmation email containing the activation link.
    /// </summary>
    /// <param name="token">The registration token embedded in the confirmation link.</param>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public async Task<bool> SendRegistrationEmail(
        string token,
        string email,
        string firstname,
        string lastname)
    {
        try
        {
            var fullName = firstname + " " + lastname;
            var confirmationLink = BuildConfirmationLink(token);

            var templateContent = await LoadTemplateAsync("RegistrationEmail");
            templateContent = templateContent.Replace("{userName}", fullName);
            templateContent = templateContent.Replace("{confirmationLink}", confirmationLink);

            var (sender, recipients) = CreateSenderAndRecipient(fullName, email);

            var subject = IsEnglish()
                ? "Confirm your registration"
                : "Bestätigen Sie Ihre Registrierung";

            var wasSent = await BrevoClient.SendAsync(
                sender,
                recipients,
                subject,
                templateContent,
                isHtml: true);

            return wasSent;
        }
        catch (Exception)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode,
                "Failed to send registration email via Brevo.");
        }
    }

    /// <summary>
    /// Sends a password-reset email containing the reset link.
    /// </summary>
    /// <param name="resetToken">The reset token embedded in the reset link.</param>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public async Task<bool> SendResetPasswordEmail(
        string resetToken,
        string email,
        string firstname,
        string lastname)
    {
        var fullName = firstname + " " + lastname;
        var resetLink = BuildResetLink(resetToken);

        var templateContent = await LoadTemplateAsync("ResetPasswordEmail");
        templateContent = templateContent.Replace("{userName}", fullName);
        templateContent = templateContent.Replace("{resetLink}", resetLink);

        var (sender, recipients) = CreateSenderAndRecipient(fullName, email);

        var subject = IsEnglish()
            ? "Reset your password"
            : "Passwort zurücksetzen";

        return await BrevoClient.SendAsync(
            sender,
            recipients,
            subject,
            templateContent,
            isHtml: true);
    }

    /// <summary>
    /// Sends a confirmation email notifying the user that their password was successfully reset.
    /// </summary>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <param name="email">The recipient's email address.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public async Task<bool> SendConfirmationResetPasswordEmail(
        string firstname,
        string lastname,
        string email)
    {
        try
        {
            var fullName = firstname + " " + lastname;
            var template = await LoadTemplateAsync("ConfirmResetPassword");

            template = template.Replace("{userName}", fullName);

            var (sender, recipients) = CreateSenderAndRecipient(fullName, email);

            var subject = IsEnglish()
                ? "Password changed!"
                : "Passwort zurückgesetzt!";

            var wasSent = await BrevoClient.SendAsync(
                sender,
                recipients,
                subject,
                template,
                isHtml: true);

            return wasSent;
        }
        catch (Exception)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode,
                "Failed to send Confirm Reset Password email via Brevo.");
        }
    }

    /// <summary>
    /// Builds the frontend URL for the password-reset page with the token as a query parameter.
    /// </summary>
    /// <param name="resetToken">The reset token to embed as a query parameter.</param>
    /// <returns>The full reset-password URL string.</returns>
    private static string BuildResetLink(string resetToken)
    {
        var baseUrl = ZootifyConfiguration.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/reset-password?token={resetToken}";
    }

    /// <summary>
    /// Builds the frontend URL for the registration confirmation page with the token as a query parameter.
    /// Throws if the configured frontend base URL is missing.
    /// </summary>
    /// <param name="token">The registration token to embed as a query parameter.</param>
    /// <returns>The full confirmation URL string.</returns>
    /// <exception cref="ZootifyException">Thrown when the frontend base URL is not configured.</exception>
    private string BuildConfirmationLink(string token)
    {
        var frontendBaseUrl = ZootifyConfiguration.FrontendBaseUrl;

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        var trimmedBaseUrl = frontendBaseUrl.TrimEnd('/');
        return $"{trimmedBaseUrl}/confirm-registration?token={token}";
    }

    /// <summary>
    /// Creates the sender and a single-recipient list using the configured Zootify sender address.
    /// Throws if sender name or address are not configured.
    /// </summary>
    /// <param name="recipientName">The recipient's full name.</param>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <returns>A tuple containing the sender and the single-recipient list.</returns>
    /// <exception cref="ZootifyException">Thrown when the sender name or address configuration is missing.</exception>
    private (Sender Sender, List<Recipient> Recipients) CreateSenderAndRecipient(
        string recipientName,
        string recipientEmail)
    {
        var senderName = ZootifyConfiguration.ZootifyName;
        var senderAddress = ZootifyConfiguration.ZootifyEmail;

        if (string.IsNullOrWhiteSpace(senderName) || string.IsNullOrWhiteSpace(senderAddress))
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode,
                "Configuration values 'Email:SenderName' and 'Email:SenderAddress' must be set.");
        }

        var sender = new Sender(senderName, senderAddress);
        var recipients = new List<Recipient>
        {
            new(recipientName, recipientEmail)
        };

        return (sender, recipients);
    }

    /// <summary>
    /// Sends an email notifying the user that a taxonomy node they created has been approved.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <param name="taxonomyName">The name of the approved taxonomy node.</param>
    /// <param name="rank">The rank of the approved taxonomy node.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public async Task<bool> SendTaxonomyApprovedEmail(
        string email,
        string firstname,
        string lastname,
        string taxonomyName,
        TaxonomyRank rank)
    {
        var fullName = firstname + " " + lastname;
        var rankLabel = TranslateRank(rank);

        var template = await LoadTemplateAsync("TaxonomyApprovedEmail");
        template = template.Replace("{userName}", fullName);
        template = template.Replace("{taxonomyName}", taxonomyName);
        template = template.Replace("{taxonomyRank}", rankLabel);

        var (sender, recipients) = CreateSenderAndRecipient(fullName, email);

        var subject = IsEnglish()
            ? $"Taxonomy approved: {taxonomyName}"
            : $"Taxonomie genehmigt: {taxonomyName}";

        return await BrevoClient.SendAsync(sender, recipients, subject, template, isHtml: true);
    }

    /// <summary>
    /// Sends an email notifying the user that a taxonomy node they created has been rejected.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="firstname">The recipient's first name.</param>
    /// <param name="lastname">The recipient's last name.</param>
    /// <param name="taxonomyName">The name of the rejected taxonomy node.</param>
    /// <param name="rank">The rank of the rejected taxonomy node.</param>
    /// <param name="comment">The rejection comment provided by the moderator or admin.</param>
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public async Task<bool> SendTaxonomyRejectedEmail(
        string email,
        string firstname,
        string lastname,
        string taxonomyName,
        TaxonomyRank rank,
        string comment)
    {
        var fullName = firstname + " " + lastname;
        var rankLabel = TranslateRank(rank);

        var template = await LoadTemplateAsync("TaxonomyRejectedEmail");
        template = template.Replace("{userName}", fullName);
        template = template.Replace("{taxonomyName}", taxonomyName);
        template = template.Replace("{taxonomyRank}", rankLabel);
        template = template.Replace("{comment}", comment);

        var (sender, recipients) = CreateSenderAndRecipient(fullName, email);

        var subject = IsEnglish()
            ? $"Taxonomy rejected: {taxonomyName}"
            : $"Taxonomie abgelehnt: {taxonomyName}";

        return await BrevoClient.SendAsync(sender, recipients, subject, template, isHtml: true);
    }

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
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public async Task<bool> SendLoanConfirmationEmail(
        string borrowerEmail,
        string borrowerName,
        string lenderName,
        string specimenName,
        string specimenLatinName,
        DateTime loanDate,
        DateTime dueDate,
        string? notes)
    {
        var dateFormat = IsEnglish() ? "MM/dd/yyyy" : "dd.MM.yyyy";

        var template = await LoadTemplateAsync("LoanConfirmationEmail");
        template = template.Replace("{borrowerName}", borrowerName);
        template = template.Replace("{lenderName}", lenderName);
        template = template.Replace("{specimenName}", specimenName);
        template = template.Replace("{specimenLatinName}", specimenLatinName);
        template = template.Replace("{loanDate}", loanDate.ToString(dateFormat));
        template = template.Replace("{dueDate}", dueDate.ToString(dateFormat));
        template = template.Replace("{notes}", string.IsNullOrWhiteSpace(notes)
            ? (IsEnglish() ? "—" : "—")
            : notes);

        var (sender, recipients) = CreateSenderAndRecipient(borrowerName, borrowerEmail);

        var subject = IsEnglish()
            ? $"Loan confirmation: {specimenName}"
            : $"Ausleihbestätigung: {specimenName}";

        return await BrevoClient.SendAsync(sender, recipients, subject, template, isHtml: true);
    }

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
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public async Task<bool> SendLoanReturnConfirmationEmail(
        string borrowerEmail,
        string borrowerName,
        string specimenName,
        string specimenLatinName,
        DateTime loanDate,
        DateTime returnDate,
        string? notes)
    {
        var dateFormat = IsEnglish() ? "MM/dd/yyyy" : "dd.MM.yyyy";

        var template = await LoadTemplateAsync("LoanReturnConfirmationEmail");
        template = template.Replace("{borrowerName}", borrowerName);
        template = template.Replace("{specimenName}", specimenName);
        template = template.Replace("{specimenLatinName}", specimenLatinName);
        template = template.Replace("{loanDate}", loanDate.ToString(dateFormat));
        template = template.Replace("{returnDate}", returnDate.ToString(dateFormat));
        template = template.Replace("{notes}", string.IsNullOrWhiteSpace(notes) ? "—" : notes);

        var (sender, recipients) = CreateSenderAndRecipient(borrowerName, borrowerEmail);

        var subject = IsEnglish()
            ? $"Return confirmed: {specimenName}"
            : $"Rückgabe bestätigt: {specimenName}";

        return await BrevoClient.SendAsync(sender, recipients, subject, template, isHtml: true);
    }

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
    /// <returns><c>true</c> if the email was sent successfully.</returns>
    public async Task<bool> SendLoanDueReminderEmail(
        string recipientEmail,
        string recipientName,
        string specimenName,
        string specimenLatinName,
        int daysUntilDue,
        DateTime dueDate,
        bool isBorrower)
    {
        var dateFormat = IsEnglish() ? "MM/dd/yyyy" : "dd.MM.yyyy";

        string daysText;
        if (IsEnglish())
        {
            daysText = daysUntilDue switch
            {
                > 1  => $"in {daysUntilDue} days",
                1    => "tomorrow",
                0    => "today",
                _    => $"{Math.Abs(daysUntilDue)} day(s) overdue",
            };
        }
        else
        {
            daysText = daysUntilDue switch
            {
                > 1  => $"in {daysUntilDue} Tagen",
                1    => "morgen",
                0    => "heute",
                _    => $"seit {Math.Abs(daysUntilDue)} Tag(en) überfällig",
            };
        }

        string reminderMessage;
        if (IsEnglish())
        {
            reminderMessage = isBorrower
                ? $"Please remember to return <strong>{specimenName}</strong> {daysText}."
                : $"<strong>{specimenName}</strong> is expected back {daysText}.";
        }
        else
        {
            reminderMessage = isBorrower
                ? $"Bitte vergessen Sie nicht, <strong>{specimenName}</strong> {daysText} zurückzugeben."
                : $"<strong>{specimenName}</strong> wird {daysText} zurückerwartet.";
        }

        var template = await LoadTemplateAsync("LoanDueReminderEmail");
        template = template.Replace("{recipientName}", recipientName);
        template = template.Replace("{specimenName}", specimenName);
        template = template.Replace("{specimenLatinName}", specimenLatinName);
        template = template.Replace("{daysText}", daysText);
        template = template.Replace("{dueDate}", dueDate.ToString(dateFormat));
        template = template.Replace("{reminderMessage}", reminderMessage);

        var (sender, recipients) = CreateSenderAndRecipient(recipientName, recipientEmail);

        var subject = IsEnglish()
            ? $"Reminder: {specimenName} due {daysText}"
            : $"Erinnerung: {specimenName} fällig {daysText}";

        return await BrevoClient.SendAsync(sender, recipients, subject, template, isHtml: true);
    }

    private static string TranslateRank(TaxonomyRank rank) => IsEnglish()
        ? rank switch
        {
            TaxonomyRank.Phylum  => "Phylum",
            TaxonomyRank.Class   => "Class",
            TaxonomyRank.Order   => "Order",
            TaxonomyRank.Family  => "Family",
            TaxonomyRank.Genus   => "Genus",
            TaxonomyRank.Species => "Species",
            _                    => rank.ToString(),
        }
        : rank switch
        {
            TaxonomyRank.Phylum  => "Stamm",
            TaxonomyRank.Class   => "Klasse",
            TaxonomyRank.Order   => "Ordnung",
            TaxonomyRank.Family  => "Familie",
            TaxonomyRank.Genus   => "Gattung",
            TaxonomyRank.Species => "Art",
            _                    => rank.ToString(),
        };

    private static bool IsEnglish() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    /// <summary>
    /// Loads an HTML email template from the assembly's embedded resources.
    /// Prefers the language-specific variant (e.g. RegistrationEmail.en.html) when available,
    /// and falls back to the default German template.
    /// </summary>
    private static async Task<string> LoadTemplateAsync(string templateName)
    {
        var assembly = typeof(BrevoProvider).Assembly;

        var localizedResourceName = IsEnglish()
            ? $"Zootify_Backend.Model.EmailTemplates.{templateName}.en.html"
            : $"Zootify_Backend.Model.EmailTemplates.{templateName}.html";

        var stream = assembly.GetManifestResourceStream(localizedResourceName)
            ?? assembly.GetManifestResourceStream(
                $"Zootify_Backend.Model.EmailTemplates.{templateName}.html");

        if (stream == null)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        await using (stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }
}
