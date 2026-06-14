using Microsoft.AspNetCore.SignalR;
using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Providers.SignalR;

public class NotificationProvider : INotificationProvider
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationProvider(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(List<UserEntity> adminsAndModerators)
    {
        var message = "New Taxonomy was created. Please check the new Taxonomy.";
        
        foreach (var user in adminsAndModerators)
        {
            
            if (user.Settings != null)
            {
                message = user.Settings.Language == "de"
                    ? "Eine neue Taxonomie wurde erstellt. Bitte überprüfen Sie die neue Taxonomie."
                    : message;   
            }
            
            await _hubContext.Clients
                .User(user.Guid.ToString())
                .SendAsync("ReceiveNotification", message);
        }
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