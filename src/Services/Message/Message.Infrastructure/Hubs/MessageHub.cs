using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Message.Application.DTOs;
using Message.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Message.Infrastructure.Hubs;

/// <summary>
/// SignalR hub for real-time messaging.
///
/// Client → Server methods:
///   - JoinConversation(conversationId)   : subscribe to a conversation group
///   - LeaveConversation(conversationId)  : unsubscribe
///   - SendMessage(SendMessageHubRequest) : send a chat message
///   - MarkAsRead(conversationId)         : mark all messages as read
///
/// Server → Client events:
///   - "ReceiveMessage"      MessageDto          : new message in a conversation group
///   - "ReadReceipt"         ReadReceiptEventDto : someone read the conversation
///   - "MessageAcknowledged" MessageDto          : sent back to the sender on success
///   - "Error"               string              : sent back to the caller on failure
/// </summary>
[Authorize]
public class MessageHub : Hub
{
    private readonly IMessageService      _messageService;
    private readonly IConversationService _convService;
    private readonly IOnlineStatusService _onlineStatus;
    private readonly ILogger<MessageHub>  _logger;

    public MessageHub(
        IMessageService      messageService,
        IConversationService convService,
        IOnlineStatusService onlineStatus,
        ILogger<MessageHub>  logger)
    {
        _messageService = messageService;
        _convService    = convService;
        _onlineStatus   = onlineStatus;
        _logger         = logger;
    }

    // ── Hub lifecycle ─────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();

        // Mark user as online in Redis (checked by MessageService before sending FCM)
        await _onlineStatus.SetOnlineAsync(userId);

        // Join personal group for direct system messages
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        _logger.LogInformation(
            "User {UserId} connected to MessageHub [ConnectionId: {ConnectionId}]",
            userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();

        await _onlineStatus.SetOfflineAsync(userId);

        _logger.LogInformation(
            "User {UserId} disconnected from MessageHub [ConnectionId: {ConnectionId}]",
            userId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    // ── Client-invokable methods ──────────────────────────────────────────────

    /// <summary>
    /// Subscribe to real-time messages for a conversation.
    /// Client must call this when opening a chat window.
    /// Verifies membership before adding to the SignalR group.
    /// </summary>
    public async Task JoinConversation(string conversationId)
    {
        var userId = GetUserId();

        var conversation = await _convService.GetConversationAsync(conversationId, userId);
        if (conversation is null)
        {
            await Clients.Caller.SendAsync("Error", "Conversation not found or access denied.");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv-{conversationId}");

        _logger.LogDebug(
            "User {UserId} joined conversation group {ConversationId}", userId, conversationId);
    }

    /// <summary>Unsubscribe from a conversation group (e.g., when closing a chat window).</summary>
    public async Task LeaveConversation(string conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv-{conversationId}");
    }

    /// <summary>
    /// Send a chat message via SignalR.
    /// Persists to MongoDB, broadcasts to conversation group, and publishes FCM events for offline recipients.
    /// Sends "MessageAcknowledged" back to the caller with the persisted MessageDto on success.
    /// </summary>
    public async Task SendMessage(SendMessageHubRequest request)
    {
        var userId = GetUserId();

        var dto = new SendMessageDto
        {
            ConversationId = request.ConversationId,
            Content        = request.Content,
            Type           = request.Type
        };

        var messageDto = await _messageService.SendMessageAsync(userId, dto);

        // Acknowledge to the sender with the server-assigned message ID and timestamp
        await Clients.Caller.SendAsync("MessageAcknowledged", messageDto);
    }

    /// <summary>
    /// Mark all messages in the conversation as read for the current user.
    /// Broadcasts a "ReadReceipt" event to all members in the conversation group.
    /// </summary>
    public async Task MarkAsRead(string conversationId)
    {
        var userId = GetUserId();
        await _messageService.MarkAsReadAsync(conversationId, userId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? Context.User?.FindFirst("sub")?.Value
            ?? throw new HubException("Unauthorized: user identity claim is missing.");

        return Guid.TryParse(claim, out var id)
            ? id
            : throw new HubException("Unauthorized: user ID is not a valid GUID.");
    }
}
