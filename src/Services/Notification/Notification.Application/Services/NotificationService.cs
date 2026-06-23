using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces;

namespace Notification.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork    _uow;
        private readonly IFcmService    _fcm;
        private readonly IOnlineTracker _tracker;
        private readonly IRealtimeService _realtime;
        private readonly IUserSettingsHttpClient _userSettings;
        private readonly ILogger<NotificationService> _logger;

        private const string ReceiveNotification = "ReceiveNotification";

        public NotificationService(
            IUnitOfWork                  uow,
            IFcmService                  fcm,
            IOnlineTracker               tracker,
            IRealtimeService             realtime,
            IUserSettingsHttpClient      userSettings,
            ILogger<NotificationService> logger)
        {
            _uow          = uow;
            _fcm          = fcm;
            _tracker      = tracker;
            _realtime     = realtime;
            _userSettings = userSettings;
            _logger       = logger;
        }

        public async Task CreateAndSendAsync(
            Guid             recipientId,
            Guid             actorId,
            NotificationType type,
            string           message,
            Guid?            referenceId = null)
        {
            // Respect the recipient's notification preferences (synced from the settings screen).
            // If the User service is unreachable, prefs is null → default to delivering.
            var prefs = await _userSettings.GetNotificationPreferencesAsync(recipientId);
            if (prefs != null && !IsTypeEnabled(type, prefs))
            {
                _logger.LogInformation(
                    "Notification of type {Type} suppressed for user {UserId} by their settings",
                    type, recipientId);
                return;
            }

            var notification = Domain.Entities.Notification.Create(
                recipientId, actorId, type, message, referenceId);

            await _uow.Notifications.AddAsync(notification);
            await _uow.SaveChangesAsync();

            var dto = MapToDto(notification);

            if (_tracker.IsOnline(recipientId))
            {
                try
                {
                    await _realtime.SendToUserAsync(recipientId, ReceiveNotification, dto);
                    _logger.LogInformation("SignalR notification sent to user {UserId}", recipientId);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "SignalR send failed for user {UserId}, falling back to FCM", recipientId);
                }
            }

            // Offline push (FCM) is additionally gated by the master "push notifications" toggle.
            if (prefs != null && !prefs.PushNotifications)
            {
                _logger.LogInformation(
                    "FCM push skipped for offline user {UserId} (push notifications disabled)", recipientId);
                return;
            }

            await SendFcmAsync(recipientId, message, dto);
        }

        /// <summary>Maps a notification type to its corresponding user preference toggle.</summary>
        private static bool IsTypeEnabled(NotificationType type, NotificationPreferences prefs) => type switch
        {
            NotificationType.FriendRequestSent     => prefs.FriendRequests,
            NotificationType.FriendRequestAccepted => prefs.FriendRequests,
            NotificationType.UserFollowed          => prefs.NewFollowers,
            NotificationType.PostLiked             => prefs.Likes,
            NotificationType.CommentCreated        => prefs.Comments,
            NotificationType.MessageReceived       => prefs.DirectMessages,
            _                                      => true
        };

        public async Task<PaginatedResponse<NotificationDto>> GetNotificationsAsync(
            Guid recipientId,
            int  page,
            int  pageSize)
        {
            var items = await _uow.Notifications.GetByRecipientAsync(recipientId, page, pageSize);
            var total = await _uow.Notifications.GetUnreadCountAsync(recipientId);

            return new PaginatedResponse<NotificationDto>
            {
                Items      = items.Select(MapToDto),
                TotalCount = total,
                Page       = page,
                PageSize   = pageSize
            };
        }

        public async Task<int> GetUnreadCountAsync(Guid recipientId) =>
            await _uow.Notifications.GetUnreadCountAsync(recipientId);

        public async Task MarkAsReadAsync(Guid notificationId, Guid currentUserId)
        {
            var notification = await _uow.Notifications.GetByIdAsync(notificationId);
            if (notification == null || notification.RecipientId != currentUserId)
                return;

            notification.MarkAsRead();
            await _uow.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(Guid recipientId)
        {
            await _uow.Notifications.MarkAllAsReadAsync(recipientId);
            await _uow.SaveChangesAsync();
        }

        // ── Private helpers ──────────────────────────────────────────────────────

        private async Task SendFcmAsync(Guid userId, string message, NotificationDto dto)
        {
            var data = new Dictionary<string, string>
            {
                ["notificationId"] = dto.Id.ToString(),
                ["type"]           = dto.Type.ToString(),
                ["referenceId"]    = dto.ReferenceId?.ToString() ?? string.Empty
            };

            await _fcm.SendAsync(userId, "New notification", message, data);
        }

        private static NotificationDto MapToDto(Domain.Entities.Notification n) =>
            new()
            {
                Id          = n.Id,
                RecipientId = n.RecipientId,
                ActorId     = n.ActorId,
                Type        = n.Type,
                Status      = n.Status,
                Message     = n.Message,
                ReferenceId = n.ReferenceId,
                CreatedAt   = n.CreatedAt,
                ReadAt      = n.ReadAt
            };
    }
}
