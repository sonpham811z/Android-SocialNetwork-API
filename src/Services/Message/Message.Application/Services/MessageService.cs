using Message.Application.DTOs;
using Message.Application.Interfaces;
using Message.Domain.Entities;
using Message.Domain.Enums;
using Message.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Message.Application.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository        _msgRepo;
    private readonly IConversationRepository   _convRepo;
    private readonly IFriendServiceClient      _friendClient;
    private readonly IOnlineStatusService      _onlineStatus;
    private readonly IMessageEventPublisher    _publisher;
    private readonly ISignalRMessageService    _signalR;
    private readonly IConversationCacheService _cache;
    private readonly ILogger<MessageService>   _logger;

    public MessageService(
        IMessageRepository        msgRepo,
        IConversationRepository   convRepo,
        IFriendServiceClient      friendClient,
        IOnlineStatusService      onlineStatus,
        IMessageEventPublisher    publisher,
        ISignalRMessageService    signalR,
        IConversationCacheService cache,
        ILogger<MessageService>   logger)
    {
        _msgRepo      = msgRepo;
        _convRepo     = convRepo;
        _friendClient = friendClient;
        _onlineStatus = onlineStatus;
        _publisher    = publisher;
        _signalR      = signalR;
        _cache        = cache;
        _logger       = logger;
    }

    public async Task<MessageDto> SendMessageAsync(Guid senderId, SendMessageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content))
            throw new ArgumentException("Message content cannot be empty.");

        // 1. Verify conversation and membership
        var conversation = await _convRepo.GetByIdAsync(dto.ConversationId)
            ?? throw new KeyNotFoundException($"Conversation {dto.ConversationId} not found.");

        if (!conversation.HasMember(senderId))
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        // 2. For 1-1 conversations, re-check friendship on every message send
        //    (covers the case where users un-friended each other after the conversation was created)
        if (conversation.Type == ConversationType.OneToOne)
        {
            var recipient = conversation.Members.First(m => m != senderId);
            if (!await _friendClient.AreFriendsAsync(senderId, recipient))
                throw new InvalidOperationException("Cannot send message to a non-friend user.");
        }

        // 3. Persist message to MongoDB
        var message = ChatMessage.Create(dto.ConversationId, senderId, dto.Content, dto.Type);
        var created = await _msgRepo.CreateAsync(message);

        // 4. Update conversation's LastMessage snapshot (denormalized for conversation list performance)
        var lastInfo = new LastMessageInfo
        {
            MessageId = created.Id,
            SenderId  = senderId,
            Content   = dto.Content,
            Timestamp = created.Timestamp
        };
        await _convRepo.UpdateLastMessageAsync(dto.ConversationId, lastInfo);
        await _cache.InvalidateAsync(dto.ConversationId);

        var messageDto = MapToDto(created);

        // 5. Real-time delivery via SignalR to all clients in the conversation group
        await _signalR.SendMessageToConversationAsync(dto.ConversationId, messageDto);

        // 6. For each offline recipient, publish RabbitMQ event → Notification Service → FCM
        var offlineRecipients = conversation.Members.Where(m => m != senderId).ToList();
        foreach (var recipientId in offlineRecipients)
        {
            if (!await _onlineStatus.IsOnlineAsync(recipientId))
            {
                await _publisher.PublishMessageCreatedAsync(new MessageCreatedEvent(
                    EventType:      "MessageCreatedEvent",
                    MessageId:      created.Id,
                    ConversationId: dto.ConversationId,
                    SenderId:       senderId,
                    RecipientId:    recipientId,
                    Content:        dto.Content,
                    Timestamp:      created.Timestamp));

                _logger.LogInformation(
                    "Published MessageCreatedEvent to offline recipient {RecipientId} in conversation {ConversationId}",
                    recipientId, dto.ConversationId);
            }
        }

        return messageDto;
    }

    public async Task<MessagePageDto> GetMessagesAsync(
        string  conversationId,
        Guid    userId,
        string? beforeMessageId,
        int     pageSize = 30)
    {
        // Verify membership
        var conversation = await _convRepo.GetByIdAsync(conversationId)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} not found.");

        if (!conversation.HasMember(userId))
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        pageSize = Math.Clamp(pageSize, 1, 100);

        // Fetch pageSize+1 to determine whether a next page exists (avoid extra COUNT query)
        var messages = (await _msgRepo.GetByConversationAsync(conversationId, beforeMessageId, pageSize + 1)).ToList();

        var hasMore = messages.Count > pageSize;
        if (hasMore) messages.RemoveAt(messages.Count - 1);

        return new MessagePageDto
        {
            Messages   = messages.Select(MapToDto),
            NextCursor = hasMore ? messages.Last().Id : null,
            HasMore    = hasMore
        };
    }

    public async Task MarkAsReadAsync(string conversationId, Guid userId)
    {
        var conversation = await _convRepo.GetByIdAsync(conversationId)
            ?? throw new KeyNotFoundException($"Conversation {conversationId} not found.");

        if (!conversation.HasMember(userId))
            throw new UnauthorizedAccessException("You are not a member of this conversation.");

        // Bulk-update ReadBy array in MongoDB for all unread messages
        await _msgRepo.MarkAsReadAsync(conversationId, userId);

        // Broadcast read receipt to other members in the conversation group via SignalR
        var receipt = new ReadReceiptEventDto
        {
            ConversationId = conversationId,
            ReaderId       = userId,
            ReadAt         = DateTime.UtcNow
        };
        await _signalR.SendReadReceiptAsync(conversationId, receipt);

        _logger.LogDebug(
            "User {UserId} marked conversation {ConversationId} as read", userId, conversationId);
    }

    // ── Mapping ──────────────────────────────────────────────────────────────

    private static MessageDto MapToDto(ChatMessage m) => new()
    {
        Id             = m.Id,
        ConversationId = m.ConversationId,
        SenderId       = m.SenderId,
        Content        = m.Content,
        Type           = m.Type,
        Timestamp      = m.Timestamp,
        ReadBy         = m.ReadBy.Select(r => new ReadReceiptDto
        {
            UserId = r.UserId,
            ReadAt = r.ReadAt
        }).ToList()
    };
}
