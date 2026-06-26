namespace Notification.Domain.Enums
{
    public enum NotificationType
    {
        FriendRequestSent,
        FriendRequestAccepted,
        UserFollowed,
        PostLiked,
        CommentCreated,
        MessageReceived,
        Mentioned
    }

    public enum NotificationStatus
    {
        Unread,
        Read
    }
}
