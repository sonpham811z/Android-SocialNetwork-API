using System.Text.Json;
using Message.Application.DTOs;
using Message.Application.Interfaces;
using Message.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Message.Infrastructure.Services;

// ── Redis Online Status ───────────────────────────────────────────────────────

/// <summary>
/// Redis-backed online status tracker.
/// Key:   "msg:online:{userId}"
/// Value: "1"  (presence marker)
/// TTL:   1 hour (acts as a safety net; explicitly deleted on hub disconnect)
///
/// This is the source of truth used by MessageService to decide whether to publish
/// a MessageCreatedEvent (FCM push) or skip it (user already receives via SignalR).
/// </summary>
public class RedisOnlineStatusService : IOnlineStatusService
{
    private readonly IDatabase       _db;
    private const    string          KeyPrefix = "msg:online:";
    private static readonly TimeSpan Ttl       = TimeSpan.FromHours(1);

    public RedisOnlineStatusService(IConnectionMultiplexer mux)
        => _db = mux.GetDatabase();

    public async Task SetOnlineAsync(Guid userId)
        => await _db.StringSetAsync($"{KeyPrefix}{userId}", "1", Ttl);

    public async Task SetOfflineAsync(Guid userId)
        => await _db.KeyDeleteAsync($"{KeyPrefix}{userId}");

    public async Task<bool> IsOnlineAsync(Guid userId)
        => await _db.KeyExistsAsync($"{KeyPrefix}{userId}");
}

// ── Redis Conversation Cache ──────────────────────────────────────────────────

/// <summary>
/// Caches ConversationDto in Redis to reduce MongoDB reads on hot paths
/// (e.g., membership checks on every message send or hub join).
/// Key: "msg:conv:{conversationId}"
/// TTL: 5 minutes (invalidated on LastMessage update or member change)
/// </summary>
public class RedisConversationCacheService : IConversationCacheService
{
    private readonly IDatabase       _db;
    private const    string          KeyPrefix = "msg:conv:";
    private static readonly TimeSpan Ttl       = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisConversationCacheService(IConnectionMultiplexer mux)
        => _db = mux.GetDatabase();

    public async Task<ConversationDto?> GetAsync(string conversationId)
    {
        var value = await _db.StringGetAsync($"{KeyPrefix}{conversationId}");
        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<ConversationDto>(value!, JsonOpts);
    }

    public async Task SetAsync(ConversationDto conversation)
    {
        var json = JsonSerializer.Serialize(conversation, JsonOpts);
        await _db.StringSetAsync($"{KeyPrefix}{conversation.Id}", json, Ttl);
    }

    public async Task InvalidateAsync(string conversationId)
        => await _db.KeyDeleteAsync($"{KeyPrefix}{conversationId}");
}

// ── SignalR Message Service ───────────────────────────────────────────────────

/// <summary>
/// Wraps IHubContext&lt;MessageHub&gt; to push real-time events to conversation groups.
/// Groups follow the naming convention: "conv-{conversationId}".
/// </summary>
public class SignalRMessageService : ISignalRMessageService
{
    private readonly IHubContext<MessageHub>    _hub;
    private readonly ILogger<SignalRMessageService> _logger;

    public SignalRMessageService(
        IHubContext<MessageHub>        hub,
        ILogger<SignalRMessageService> logger)
    {
        _hub    = hub;
        _logger = logger;
    }

    public async Task SendMessageToConversationAsync(string conversationId, MessageDto message)
    {
        await _hub.Clients
            .Group($"conv-{conversationId}")
            .SendAsync("ReceiveMessage", message);

        _logger.LogDebug(
            "Broadcasted message {MessageId} to conversation group {ConversationId}",
            message.Id, conversationId);
    }

    public async Task SendReadReceiptAsync(string conversationId, ReadReceiptEventDto receipt)
    {
        await _hub.Clients
            .Group($"conv-{conversationId}")
            .SendAsync("ReadReceipt", receipt);
    }
}
