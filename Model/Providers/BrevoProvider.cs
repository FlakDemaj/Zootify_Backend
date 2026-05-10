using SendWithBrevo;
using Zootify_Backend.Configuration;
using Zootify_Backend.Enums;

namespace Zootify_Backend.Models.Providers;

/// <summary>
/// Email provider implementation using Brevo (formerly Sendinblue) for sending
/// transactional emails such as registration confirmations.
/// </summary>
public class BrevoProvider : IBrevoProvider
{
    private BrevoClient BrevoClient { get; }

    public BrevoProvider(BrevoClient brevoClient)
    {
        BrevoClient = brevoClient;
    }

    /// <summary>
    /// Sends a registration confirmation email containing a link with the user's GUID.
    /// The link points to the frontend, which then triggers backend confirmation.
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
    
    private static string BuildResetLink(string resetToken)
    {
        var baseUrl = ZootifyConfiguration.FrontendBaseUrl.TrimEnd('/');
        return $"{baseUrl}/reset-password?token={resetToken}";
    }

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