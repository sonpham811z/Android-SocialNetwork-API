using System.Collections.Concurrent;
using System.Text.Json;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Domain.Interfaces;
using Notification.Infrastructure.Hubs;

namespace Notification.Infrastructure.Services
{
    // ── Online Tracker ───────────────────────────────────────────────────────────

    /// <summary>
    /// In-memory tracker for connected SignalR users.
    /// For multi-instance deployments, replace with a Redis-backed implementation.
    /// </summary>
    public class OnlineTracker : IOnlineTracker
    {
        // userId → set of connectionIds (a user may have multiple tabs/devices)
        private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections = new();
        private readonly object _lock = new();

        public void AddConnection(Guid userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_connections.TryGetValue(userId, out var set))
                {
                    set = [];
                    _connections[userId] = set;
                }
                set.Add(connectionId);
            }
        }

        public void RemoveConnection(Guid userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_connections.TryGetValue(userId, out var set))
                    return;

                set.Remove(connectionId);

                if (set.Count == 0)
                    _connections.TryRemove(userId, out _);
            }
        }

        public bool IsOnline(Guid userId) =>
            _connections.TryGetValue(userId, out var set) && set.Count > 0;
    }

    // ── SignalR Realtime Service ──────────────────────────────────────────────────

    public class SignalRRealtimeService : IRealtimeService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRRealtimeService(IHubContext<NotificationHub> hubContext) =>
            _hubContext = hubContext;

        public async Task SendToUserAsync(Guid userId, string method, object payload) =>
            await _hubContext.Clients
                .Group($"user-{userId}")
                .SendAsync(method, payload);
    }

    // ── FCM Service ──────────────────────────────────────────────────────────────

    public class FcmService : IFcmService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<FcmService> _logger;

        public FcmService(IUnitOfWork uow, ILogger<FcmService> logger)
        {
            _uow    = uow;
            _logger = logger;
        }

        public async Task SendAsync(
            Guid                       userId,
            string                     title,
            string                     body,
            Dictionary<string, string>? data = null)
        {
            var tokens = (await _uow.DeviceTokens.GetByUserIdAsync(userId)).ToList();

            if (tokens.Count == 0)
            {
                _logger.LogDebug("No device tokens for user {UserId}", userId);
                return;
            }

            var messaging = FirebaseMessaging.DefaultInstance;
            var staleTokens = new List<string>();

            foreach (var deviceToken in tokens)
            {
                var message = new Message
                {
                    Token = deviceToken.Token,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body  = body
                    },
                    Data = data ?? []
                };

                try
                {
                    await messaging.SendAsync(message);
                    _logger.LogInformation(
                        "FCM sent to user {UserId} token {Token}", userId, deviceToken.Token[..10]);
                }
                catch (FirebaseMessagingException ex)
                    when (ex.MessagingErrorCode is MessagingErrorCode.Unregistered
                                               or MessagingErrorCode.InvalidArgument)
                {
                    // Token is invalid/expired — clean it up
                    staleTokens.Add(deviceToken.Token);
                    _logger.LogWarning(
                        "Stale FCM token removed for user {UserId}: {Error}", userId, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "FCM send error for user {UserId}", userId);
                }
            }

            // Remove stale tokens
            foreach (var stale in staleTokens)
            {
                await _uow.DeviceTokens.DeleteByTokenAsync(stale);
            }

            if (staleTokens.Count > 0)
                await _uow.SaveChangesAsync();
        }
    }

    // ── Post HTTP Client ─────────────────────────────────────────────────────────

    public class PostHttpClient : IPostHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<PostHttpClient> _logger;

        public PostHttpClient(HttpClient client, ILogger<PostHttpClient> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<Guid?> GetPostOwnerAsync(Guid postId)
        {
            try
            {
                var response = await _client.GetAsync($"api/post/{postId}");
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("data", out var dataEl) &&
                    dataEl.TryGetProperty("userId", out var userIdEl) &&
                    Guid.TryParse(userIdEl.GetString(), out var ownerId))
                {
                    return ownerId;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get post owner for post {PostId}", postId);
                return null;
            }
        }
    }
}
