using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Zootify_Backend.Model.Providers.SignalR;

/// <summary>
/// SignalR hub that authenticated clients connect to in order to receive real-time notifications.
/// </summary>
[Authorize]
public class NotificationHub : Hub;