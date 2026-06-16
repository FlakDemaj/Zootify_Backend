using Microsoft.AspNetCore.SignalR;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Providers.SignalR;

/// <summary>
/// <see cref="INotificationProvider"/> implementation that dispatches notifications to clients through
/// the SignalR <see cref="NotificationHub"/>.
/// </summary>
public class NotificationProvider : INotificationProvider
{
    /// <summary>The hub context used to send messages to connected clients.</summary>
    private readonly IHubContext<NotificationHub> _hubContext;

    /// <summary>
    /// Initializes the provider with the notification hub context.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context used to reach connected clients.</param>
    public NotificationProvider(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc/>
    public Task SendNotificationAsync(List<UserEntity> adminsAndModerators)
    {
        return ParallelNotifications.RunAsync(adminsAndModerators, user =>
        {
            var message = user.Settings?.Language == "de"
                ? "Eine neue Taxonomie wurde erstellt. Bitte überprüfen Sie die neue Taxonomie."
                : "New Taxonomy was created. Please check the new Taxonomy.";

            return _hubContext.Clients
                .User(user.Guid.ToString())
                .SendAsync("ReceiveNotification", message);
        });
    }

    /// <summary>
    /// Sends a localized push notification to a single user over SignalR, selecting the message
    /// variant based on the user's language preference (German by default).
    /// </summary>
    /// <param name="user">The recipient; their <c>Settings.Language</c> selects the message variant.</param>
    /// <param name="germanMessage">The German message text.</param>
    /// <param name="englishMessage">The English message text.</param>
    public async Task SendNotificationAsync(UserEntity user, string germanMessage, string englishMessage)
    {
        var message = user.Settings.Language == "en" ? englishMessage : germanMessage;

        await _hubContext.Clients
            .User(user.Guid.ToString())
            .SendAsync("ReceiveNotification", message);
    }
}