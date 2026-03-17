using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Friend.Application.Interfaces;
using RabbitMQ.Client;

namespace Friend.Infrastructure.Messaging
{
    public class RabbitMqPublisher : IMessagePublisher, IDisposable
    {
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        
        // SemaphoreSlim để chống đụng độ luồng (Thread-safe)
        private readonly SemaphoreSlim _channelLock = new SemaphoreSlim(1, 1);

        public RabbitMqPublisher(IOptions<RabbitMQSettings> settings, ILogger<RabbitMqPublisher> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            // Khởi tạo kết nối (bỏ qua await ở constructor)
            _ = InitializeRabbitMQAsync();
        }

        private async Task InitializeRabbitMQAsync()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Host,
                    Port = _settings.Port,
                    UserName = _settings.Username,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange: _settings.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                );

                _logger.LogInformation("RabbitMQ connection established successfully for Friend service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ connection for Friend service");
                throw;
            }
        }

        public async Task PublishFriendRequestSentAsync(Guid requestId, Guid senderId, Guid receiverId)
        {
            var message = new
            {
                EventType = "FriendRequestSent",
                RequestId = requestId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Timestamp = DateTime.UtcNow
            };

            await PublishMessageAsync("friend.request.sent", message);
        }

        public async Task PublishFriendRequestAcceptedAsync(Guid requestId, Guid senderId, Guid receiverId)
        {
            var message = new
            {
                EventType = "FriendRequestAccepted",
                RequestId = requestId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Timestamp = DateTime.UtcNow
            };

            await PublishMessageAsync("friend.request.accepted", message);
        }

        public async Task PublishFriendRequestDeclinedAsync(Guid requestId, Guid senderId, Guid receiverId)
        {
            var message = new
            {
                EventType = "FriendRequestDeclined",
                RequestId = requestId,
                SenderId = senderId,
                ReceiverId = receiverId,
                Timestamp = DateTime.UtcNow
            };

            await PublishMessageAsync("friend.request.declined", message);
        }

        public async Task PublishFriendRemovedAsync(Guid userId, Guid targetUserId)
        {
            var message = new
            {
                EventType = "FriendRemoved",
                UserId = userId,
                TargetUserId = targetUserId,
                Timestamp = DateTime.UtcNow
            };

            await PublishMessageAsync("friend.removed", message);
        }

        public async Task PublishUserFollowedAsync(Guid followerId, Guid followeeId)
        {
            var message = new
            {
                EventType = "UserFollowed",
                FollowerId = followerId,
                FolloweeId = followeeId,
                Timestamp = DateTime.UtcNow
            };

            await PublishMessageAsync("friend.followed", message);
        }

        public async Task PublishUserUnfollowedAsync(Guid followerId, Guid followeeId)
        {
            var message = new
            {
                EventType = "UserUnfollowed",
                FollowerId = followerId,
                FolloweeId = followeeId,
                Timestamp = DateTime.UtcNow
            };

            await PublishMessageAsync("friend.unfollowed", message);
        }

        public async Task PublishUserBlockedAsync(Guid blockerId, Guid blockedId)
        {
            var message = new
            {
                EventType = "UserBlocked",
                BlockerId = blockerId,
                BlockedId = blockedId,
                Timestamp = DateTime.UtcNow
            };

            await PublishMessageAsync("friend.blocked", message);
        }

        private async Task PublishMessageAsync(string routingKey, object message)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogWarning("RabbitMQ channel is closed. Attempting to reconnect...");
                await InitializeRabbitMQAsync();
            }

            try
            {
                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    Type = message.GetType().GetProperty("EventType")?.GetValue(message)?.ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                await _channelLock.WaitAsync();
                try
                {
                    await _channel.BasicPublishAsync(
                        exchange: _settings.ExchangeName,
                        routingKey: routingKey,
                        mandatory: false,
                        basicProperties: properties,
                        body: body
                    );
                }
                finally
                {
                    _channelLock.Release();
                }

                _logger.LogInformation("Published message to {RoutingKey}: {Json}", routingKey, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to {RoutingKey}", routingKey);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _channel?.CloseAsync();
                _channel?.Dispose();
                _connection?.CloseAsync();
                _connection?.Dispose();
                _logger.LogInformation("RabbitMQ connection disposed in Friend service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ connection");
            }
        }
    }
}