using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Domain.Enums;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.Infrastructure.Messaging
{
    public class RabbitMQEventConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQEventConsumer> _logger;
        private IConnection? _connection;
        private IChannel?    _channel;

        public RabbitMQEventConsumer(
            IServiceProvider                serviceProvider,
            IOptions<RabbitMQSettings>      settings,
            ILogger<RabbitMQEventConsumer>  logger)
        {
            _serviceProvider = serviceProvider;
            _settings        = settings.Value;
            _logger          = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
                _logger.LogInformation("Notification RabbitMQ Consumer is stopping"));

            await InitializeRabbitMQAsync();

            var consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body      = ea.Body.ToArray();
                    var message   = Encoding.UTF8.GetString(body);
                    var eventType = ea.BasicProperties.Type ?? string.Empty;

                    _logger.LogInformation(
                        "Received event {EventType} [{RoutingKey}]", eventType, ea.RoutingKey);

                    using var scope = _serviceProvider.CreateAsyncScope();
                    var notificationService =
                        scope.ServiceProvider.GetRequiredService<INotificationService>();
                    var postHttpClient =
                        scope.ServiceProvider.GetRequiredService<IPostHttpClient>();

                    await HandleEventAsync(eventType, message, notificationService, postHttpClient);

                    await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification event");
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await _channel!.BasicConsumeAsync(
                queue:   _settings.QueueName,
                autoAck: false,
                consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
                await Task.Delay(1000, stoppingToken);
        }

        private async Task InitializeRabbitMQAsync()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName                = _settings.Host,
                    Port                    = _settings.Port,
                    UserName                = _settings.Username,
                    Password                = _settings.Password,
                    VirtualHost             = _settings.VirtualHost,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval  = TimeSpan.FromSeconds(10)
                };

                _connection = await factory.CreateConnectionAsync();
                _channel    = await _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange:   _settings.ExchangeName,
                    type:       ExchangeType.Topic,
                    durable:    true,
                    autoDelete: false);

                await _channel.QueueDeclareAsync(
                    queue:      _settings.QueueName,
                    durable:    true,
                    exclusive:  false,
                    autoDelete: false,
                    arguments:  null);

                // ── Bind routing keys ───────────────────────────────────────────
                // Friend events
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "friend.request.sent");
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "friend.request.accepted");
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "friend.followed");

                // Post events
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "post.liked");
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "comment.created");

                // Message Service events
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "message.created");

                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger.LogInformation("Notification RabbitMQ initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Notification RabbitMQ");
                throw;
            }
        }

        private async Task HandleEventAsync(
            string                eventType,
            string                message,
            INotificationService  notificationService,
            IPostHttpClient       postHttpClient)
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            switch (eventType)
            {
                // ── Friend events ─────────────────────────────────────────────

                case "FriendRequestSent":
                {
                    var e = JsonSerializer.Deserialize<FriendRequestSentEventDto>(message, opts);
                    if (e is null) break;

                    await notificationService.CreateAndSendAsync(
                        recipientId: e.ReceiverId,
                        actorId:     e.SenderId,
                        type:        NotificationType.FriendRequestSent,
                        message:     "sent you a friend request.",
                        referenceId: e.RequestId);
                    break;
                }

                case "FriendRequestAccepted":
                {
                    var e = JsonSerializer.Deserialize<FriendRequestAcceptedEventDto>(message, opts);
                    if (e is null) break;

                    // Notify the original sender that their request was accepted
                    await notificationService.CreateAndSendAsync(
                        recipientId: e.SenderId,
                        actorId:     e.ReceiverId,
                        type:        NotificationType.FriendRequestAccepted,
                        message:     "accepted your friend request.",
                        referenceId: e.RequestId);
                    break;
                }

                case "UserFollowed":
                {
                    var e = JsonSerializer.Deserialize<UserFollowedEventDto>(message, opts);
                    if (e is null) break;

                    await notificationService.CreateAndSendAsync(
                        recipientId: e.FolloweeId,
                        actorId:     e.FollowerId,
                        type:        NotificationType.UserFollowed,
                        message:     "started following you.");
                    break;
                }

                // ── Post events ───────────────────────────────────────────────

                case "PostLiked":
                {
                    var e = JsonSerializer.Deserialize<PostLikedEventDto>(message, opts);
                    if (e is null) break;

                    var ownerId = await postHttpClient.GetPostOwnerAsync(e.PostId);
                    if (ownerId == null || ownerId == e.UserId) break; // skip self-like

                    await notificationService.CreateAndSendAsync(
                        recipientId: ownerId.Value,
                        actorId:     e.UserId,
                        type:        NotificationType.PostLiked,
                        message:     "liked your post.",
                        referenceId: e.PostId);
                    break;
                }

                case "CommentCreated":
                {
                    var e = JsonSerializer.Deserialize<CommentCreatedEventDto>(message, opts);
                    if (e is null) break;

                    var ownerId = await postHttpClient.GetPostOwnerAsync(e.PostId);
                    if (ownerId == null || ownerId == e.UserId) break; // skip self-comment

                    await notificationService.CreateAndSendAsync(
                        recipientId: ownerId.Value,
                        actorId:     e.UserId,
                        type:        NotificationType.CommentCreated,
                        message:     "commented on your post.",
                        referenceId: e.PostId);
                    break;
                }

                // ── Message events ────────────────────────────────────────────

                case "MessageCreatedEvent":
                {
                    var e = JsonSerializer.Deserialize<MessageCreatedEventDto>(message, opts);
                    if (e is null) break;

                    // Only notify the designated recipient (Message Service publishes one event per offline user)
                    await notificationService.CreateAndSendAsync(
                        recipientId: e.RecipientId,
                        actorId:     e.SenderId,
                        type:        NotificationType.MessageReceived,
                        message:     $"sent you a message.",
                        referenceId: null);
                    break;
                }

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }
        }

        public override void Dispose()
        {
            _channel?.CloseAsync();
            _channel?.Dispose();
            _connection?.CloseAsync();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
