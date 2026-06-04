using Message.Application.DTOs;

namespace Message.Application.Interfaces;

// ── Application service interfaces ───────────────────────────────────────────

public interface IConversationService
{
    Task<ConversationDto>              CreateOneToOneAsync(Guid currentUserId, Guid targetUserId);
    Task<ConversationDto>              CreateGroupAsync(Guid currentUserId, CreateGroupConversationDto dto);
    Task<IEnumerable<ConversationDto>> GetUserConversationsAsync(Guid userId);
    Task<ConversationDto?>             GetConversationAsync(string conversationId, Guid userId);
}

public interface IMessageService
{
    Task<MessageDto>     SendMessageAsync(Guid senderId, SendMessageDto dto);
    Task<MessagePageDto> GetMessagesAsync(string conversationId, Guid userId, string? beforeMessageId, int pageSize = 30);
    Task                 MarkAsReadAsync(string conversationId, Guid userId);
}

// ── Infrastructure interfaces (implemented in Infrastructure layer) ───────────

/// <summary>
/// gRPC client interface to Friend Service.
/// Verifies that two users have an active friendship before allowing 1-1 conversations or messages.
/// Implement with <c>GrpcFriendServiceClient</c> in the Infrastructure layer.
/// The Friend Service must expose a gRPC endpoint defined in <c>Protos/friendship.proto</c>.
/// </summary>
public interface IFriendServiceClient
{
    /// <returns><c>true</c> if both users are mutual friends; <c>false</c> otherwise.</returns>
    Task<bool> AreFriendsAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
}

/// <summary>Redis-backed online status tracker shared across Message service instances.</summary>
public interface IOnlineStatusService
{
    Task       SetOnlineAsync(Guid userId);
    Task       SetOfflineAsync(Guid userId);
    Task<bool> IsOnlineAsync(Guid userId);
}

/// <summary>Redis-backed short-lived cache for conversation metadata (avoids hot-path MongoDB reads).</summary>
public interface IConversationCacheService
{
    Task<ConversationDto?> GetAsync(string conversationId);
    Task                   SetAsync(ConversationDto conversation);
    Task                   InvalidateAsync(string conversationId);
}

/// <summary>
/// Publishes message-related domain events to RabbitMQ (topic exchange: social.network.events).
/// Consumed by Notification Service to deliver FCM push notifications to offline users.
/// </summary>
public interface IMessageEventPublisher
{
    Task PublishMessageCreatedAsync(MessageCreatedEvent @event);
}

/// <summary>SignalR hub context wrapper for pushing real-time events to conversation groups.</summary>
public interface ISignalRMessageService
{
    /// <summary>Broadcast a new message to all clients in the conversation's SignalR group.</summary>
    Task SendMessageToConversationAsync(string conversationId, MessageDto message);

    /// <summary>Notify clients in the conversation that a user has read all messages.</summary>
    Task SendReadReceiptAsync(string conversationId, ReadReceiptEventDto receipt);
}

/// <summary>Generates Agora RTC tokens for voice/video calls.</summary>
public interface IAgoraTokenService
{
    string GenerateRtcToken(string channelName, string uid, bool isPublisher);
}

// ── Domain events ─────────────────────────────────────────────────────────────

/// <summary>
/// Published to RabbitMQ (routing key: "message.created") when a message is sent
/// to a recipient who is currently offline (not connected to MessageHub).
/// </summary>
public record MessageCreatedEvent(
    string   EventType,
    string   MessageId,
    string   ConversationId,
    Guid     SenderId,
    Guid     RecipientId,
    string   Content,
    DateTime Timestamp);
