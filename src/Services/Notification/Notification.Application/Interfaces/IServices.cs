using Notification.Application.DTOs;
using Notification.Domain.Enums;

namespace Notification.Application.Interfaces
{
    public interface INotificationService
    {
        Task CreateAndSendAsync(
            Guid             recipientId,
            Guid             actorId,
            NotificationType type,
            string           message,
            Guid?            referenceId = null);

        Task<PaginatedResponse<NotificationDto>> GetNotificationsAsync(
            Guid recipientId,
            int  page,
            int  pageSize);

        Task<int> GetUnreadCountAsync(Guid recipientId);
        Task MarkAsReadAsync(Guid notificationId, Guid currentUserId);
        Task MarkAllAsReadAsync(Guid recipientId);
    }

    /// <summary>Abstraction over SignalR so Application layer stays framework-agnostic.</summary>
    public interface IRealtimeService
    {
        Task SendToUserAsync(Guid userId, string method, object payload);
    }

    public interface IFcmService
    {
        Task SendAsync(Guid userId, string title, string body, Dictionary<string, string>? data = null);
    }

    /// <summary>Tracks which users are currently connected via SignalR.</summary>
    public interface IOnlineTracker
    {
        void AddConnection(Guid userId, string connectionId);
        void RemoveConnection(Guid userId, string connectionId);
        bool IsOnline(Guid userId);
    }

    public interface IPostHttpClient
    {
        /// <summary>Returns the ownerId (UserId) of the given post, or null if not found.</summary>
        Task<Guid?> GetPostOwnerAsync(Guid postId);
    }

    /// <summary>Reads a user's notification preferences from the User service.</summary>
    public interface IUserSettingsHttpClient
    {
        /// <summary>
        /// Returns the recipient's notification preferences, or <c>null</c> if the User
        /// service is unreachable or has no record (caller should then default to sending).
        /// </summary>
        Task<NotificationPreferences?> GetNotificationPreferencesAsync(Guid userId);
    }
}
