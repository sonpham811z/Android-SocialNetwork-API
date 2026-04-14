using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;

namespace Notification.Infrastructure.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly IOnlineTracker _tracker;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(IOnlineTracker tracker, ILogger<NotificationHub> logger)
        {
            _tracker = tracker;
            _logger  = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                Context.Abort();
                return;
            }

            _tracker.AddConnection(userId, Context.ConnectionId);

            // Each user joins a named group so we can target them by userId
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

            _logger.LogInformation(
                "User {UserId} connected (connectionId={ConnectionId})", userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId != Guid.Empty)
            {
                _tracker.RemoveConnection(userId, Context.ConnectionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");

                _logger.LogInformation(
                    "User {UserId} disconnected (connectionId={ConnectionId})", userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private Guid GetUserId()
        {
            var raw = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? Context.User?.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }
}
