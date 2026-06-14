using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Providers.SignalR;

public interface INotificationProvider
{
    Task SendNotificationAsync(List<UserEntity> adminsAndModerators);

    /// <summary>
    /// Sends a localized push notification to a single user over SignalR, selecting the message
    /// variant based on the user's language preference (German by default).
    /// </summary>
    /// <param name="user">The recipient; their <c>Settings.Language</c> selects the message variant.</param>
    /// <param name="germanMessage">The German message text.</param>
    /// <param name="englishMessage">The English message text.</param>
    Task SendNotificationAsync(UserEntity user, string germanMessage, string englishMessage);
}