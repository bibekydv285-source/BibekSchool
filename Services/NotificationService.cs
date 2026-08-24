using BibekSchool.Data;
using BibekSchool.Models;
using Microsoft.EntityFrameworkCore;

namespace BibekSchool.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Notification?> GetNotificationByIdAsync(int id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, string? role = null, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.TargetUserId == userId || (n.TargetRole == role && n.IsGlobal));

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId, string? role = null)
        {
            return await _context.Notifications
                .Where(n => (n.TargetUserId == userId || (n.TargetRole == role && n.IsGlobal)) && !n.IsRead)
                .CountAsync();
        }

        public async Task<Notification> CreateNotificationAsync(string title, string message, string? targetUserId = null, string? targetRole = null, bool isGlobal = false, string? referenceLink = null, string? createdBy = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = "Info",
                TargetUserId = targetUserId,
                TargetRole = targetRole,
                IsGlobal = isGlobal,
                ReferenceLink = referenceLink,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy ?? "System"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        public async Task<bool> MarkAsReadAsync(int id, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && (n.TargetUserId == userId || n.IsGlobal));

            if (notification == null) return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(string userId, string? role = null)
        {
            var notifications = await _context.Notifications
                .Where(n => (n.TargetUserId == userId || (n.TargetRole == role && n.IsGlobal)) && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteNotificationAsync(int id, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && (n.TargetUserId == userId || n.IsGlobal));

            if (notification == null) return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}