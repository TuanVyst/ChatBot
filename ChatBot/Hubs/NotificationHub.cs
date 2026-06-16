using Microsoft.AspNetCore.SignalR;
using DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatBot.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly AppDbContext _context;

        public NotificationHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task MarkNotificationRead(int notificationId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var accountId))
                return;

            var notification = await _context.StudentNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.AccountId == accountId && !n.IsRead);

            if (notification == null)
                return;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
