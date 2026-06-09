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
}