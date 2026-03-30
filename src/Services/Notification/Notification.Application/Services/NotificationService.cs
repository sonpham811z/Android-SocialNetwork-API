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
        private readonly ILogger<NotificationService> _logger;

        private const string ReceiveNotification = "ReceiveNotification";

        public NotificationService(
            IUnitOfWork                  uow,
            IFcmService                  fcm,
            IOnlineTracker               tracker,
            IRealtimeService             realtime,
            ILogger<NotificationService> logger)
        {
            _uow      = uow;
            _fcm      = fcm;
            _tracker  = tracker;
            _realtime = realtime;
            _logger   = logger;
        }

        public async Task CreateAndSendAsync(
            Guid             recipientId,
            Guid             actorId,
            NotificationType type,
            string           message,
            Guid?            referenceId = null)
        {
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

            await SendFcmAsync(recipientId, message, dto);
        }

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
