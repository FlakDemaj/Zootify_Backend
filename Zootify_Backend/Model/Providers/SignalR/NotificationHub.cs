using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Zootify_Backend.Model.Providers.SignalR;

[Authorize]
public class NotificationHub : Hub;