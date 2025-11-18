using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace unipos_basic_backend.src.Repositories
{
    [Authorize]
    public class NotificationHub: Hub
    {
        public async Task SendNotificationToAll(string message)
        {
            await Clients.All.SendAsync("newNotification", message);
        }
    }
}