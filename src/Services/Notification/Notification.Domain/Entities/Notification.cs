using Notification.Domain.Enums;

namespace Notification.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; private set; }

        /// <summary>User who receives this notification.</summary>
        public Guid RecipientId { get; private set; }

        /// <summary>User who triggered the action (liker, commenter, sender…).</summary>
        public Guid ActorId { get; private set; }

        public NotificationType Type { get; private set; }
        public NotificationStatus Status { get; private set; }

        /// <summary>Human-readable message, e.g. "John liked your post".</summary>
        public string Message { get; private set; } = string.Empty;

        /// <summary>Related entity id: PostId, FriendRequestId, etc.</summary>
        public Guid? ReferenceId { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? ReadAt { get; private set; }
        public bool IsDeleted { get; private set; }

        private Notification() { }

        public static Notification Create(
            Guid recipientId,
            Guid actorId,
            NotificationType type,
            string message,
            Guid? referenceId = null)
        {
            return new Notification
            {
                Id          = Guid.NewGuid(),
                RecipientId = recipientId,
                ActorId     = actorId,
                Type        = type,
                Status      = NotificationStatus.Unread,
                Message     = message,
                ReferenceId = referenceId,
                CreatedAt   = DateTime.UtcNow,
                IsDeleted   = false
            };
        }

        public void MarkAsRead()
        {
            Status = NotificationStatus.Read;
            ReadAt = DateTime.UtcNow;
        }

        public void SoftDelete() => IsDeleted = true;
    }
}
