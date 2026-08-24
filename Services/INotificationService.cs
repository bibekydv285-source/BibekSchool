using BibekSchool.Models;

namespace BibekSchool.Services
{
    public interface INotificationService
    {
        Task<Notification?> GetNotificationByIdAsync(int id);
        Task<List<Notification>> GetUserNotificationsAsync(string userId, string? role = null, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync(string userId, string? role = null);
        Task<Notification> CreateNotificationAsync(string title, string message, string? targetUserId = null, string? targetRole = null, bool isGlobal = false, string? referenceLink = null, string? createdBy = null);
        Task<bool> MarkAsReadAsync(int id, string userId);
        Task<bool> MarkAllAsReadAsync(string userId, string? role = null);
        Task<bool> DeleteNotificationAsync(int id, string userId);
    }
}