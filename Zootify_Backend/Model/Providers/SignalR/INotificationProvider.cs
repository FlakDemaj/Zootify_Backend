using Zootify_Backend.Model.DataAccess.Entitys;

namespace Zootify_Backend.Model.Providers.SignalR;

public interface INotificationProvider
{
    Task SendNotificationAsync(List<UserEntity> adminsAndModerators);
}