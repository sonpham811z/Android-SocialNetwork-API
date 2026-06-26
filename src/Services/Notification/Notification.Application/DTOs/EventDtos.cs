namespace Notification.Application.DTOs
{
    // ── Friend Service Events ────────────────────────────────────────────────────

    public record FriendRequestSentEventDto(
        string EventType,
        Guid   RequestId,
        Guid   SenderId,
        Guid   ReceiverId,
        DateTime Timestamp);

    public record FriendRequestAcceptedEventDto(
        string EventType,
        Guid   RequestId,
        Guid   SenderId,
        Guid   ReceiverId,
        DateTime Timestamp);

    public record UserFollowedEventDto(
        string EventType,
        Guid   FollowerId,
        Guid   FolloweeId,
        DateTime Timestamp);

    // ── Message Service Events ───────────────────────────────────────────────────

    public record MessageCreatedEventDto(
        string   EventType,
        string   MessageId,
        string   ConversationId,
        Guid     SenderId,
        Guid     RecipientId,
        string   Content,
        DateTime Timestamp);

    // ── Post Service Events ──────────────────────────────────────────────────────

    public record PostLikedEventDto(
        string EventType,
        Guid   PostId,
        Guid   UserId,      // liker
        DateTime Timestamp);

    public record CommentCreatedEventDto(
        string EventType,
        Guid   CommentId,
        Guid   PostId,
        Guid   UserId,      // commenter
        string Content,
        DateTime Timestamp);

    public record UserMentionedEventDto(
        string EventType,
        Guid   PostId,
        Guid   ActorId,     // người nhắc (tác giả post/comment)
        Guid   RecipientId, // người được nhắc
        bool   IsComment,
        DateTime Timestamp);
}
