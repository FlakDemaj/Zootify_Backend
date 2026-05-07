using Microsoft.Extensions.Configuration;
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

            var templateContent = await LoadTemplateAsync();
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
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Failed to send registration email via Brevo.",
                exception);
        }
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

    private static async Task<string> LoadTemplateAsync()
    {
        var assembly = typeof(BrevoProvider).Assembly;
        var resourceName = "Zootify_Backend.Models.EmailTemplates.RegistrationEmail.html";

        await using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            throw new ZootifyException(ErrorCode.DefaultErrorCode);
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}