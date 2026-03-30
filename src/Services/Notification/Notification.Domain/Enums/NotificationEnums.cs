namespace Notification.Domain.Enums
{
    public enum NotificationType
    {
        FriendRequestSent,
        FriendRequestAccepted,
        UserFollowed,
        PostLiked,
        CommentCreated,
        MessageReceived
    }

    public enum NotificationStatus
    {
        Unread,
        Read
    }
}
