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
    public BrevoProvider(BrevoClient brevoClient)
    {
        BrevoClient = brevoClient;
    }

    /// <summary>
    /// Sends a registration confirmation email containing the activation link.
    /// </summary>
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

            var wasSent = await BrevoClient.SendAsync(
                sender,
                recipients,
                "Bestätigen Sie Ihre Registrierung",
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

        return await BrevoClient.SendAsync(
            sender,
            recipients,
            "Passwort zurücksetzen",
            templateContent,
            isHtml: true);
    }

    /// <summary>
    /// Sends a confirmation email notifying the user that their password was successfully reset.
    /// </summary>
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

            var wasSent = await BrevoClient.SendAsync(
                sender,
                recipients,
                "Passwort zurückgesetzt!",
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
    private static string BuildResetLink(string resetToken)
    {
        var baseUrl = ZootifyConfiguration.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/reset-password?token={resetToken}";
    }

    /// <summary>
    /// Builds the frontend URL for the registration confirmation page with the token as a query parameter.
    /// Throws if the configured frontend base URL is missing.
    /// </summary>
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
    /// Loads an HTML email template from the assembly's embedded resources.
    /// Throws if the resource cannot be found.
    /// </summary>
    private static async Task<string> LoadTemplateAsync(string templateName)
    {
        var assembly = typeof(BrevoProvider).Assembly;
        var resourceName = $"Zootify_Backend.Model.EmailTemplates.{templateName}.html";

        await using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
