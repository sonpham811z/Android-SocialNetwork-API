using System.Text;
using System.Text.Json;
using Message.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Message.Infrastructure.Messaging;

/// <summary>
/// Publishes MessageCreatedEvent to RabbitMQ topic exchange "social.network.events"
/// with routing key "message.created". Consumed by Notification Service for FCM push delivery.
/// </summary>
public class RabbitMQMessagePublisher : IMessageEventPublisher, IAsyncDisposable
{
    private readonly RabbitMQSettings                  _settings;
    private readonly ILogger<RabbitMQMessagePublisher> _logger;
    private          IConnection?                      _connection;
    private          IChannel?                         _channel;
    private readonly SemaphoreSlim                     _lock = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMQMessagePublisher(
        IOptions<RabbitMQSettings>             settings,
        ILogger<RabbitMQMessagePublisher>      logger)
    {
        _settings = settings.Value;
        _logger   = logger;
    }

    public async Task PublishMessageCreatedAsync(MessageCreatedEvent @event)
    {
        await EnsureChannelAsync();

        var body  = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(@event, _jsonOpts));
        var props = new BasicProperties
        {
            Persistent  = true,
            Type        = "MessageCreatedEvent",
            ContentType = "application/json"
        };

        await _channel!.BasicPublishAsync(
            exchange:        _settings.ExchangeName,
            routingKey:      "message.created",
            mandatory:       false,
            basicProperties: props,
            body:            body);

        _logger.LogInformation(
            "Published MessageCreatedEvent → recipient {RecipientId}, conversation {ConversationId}",
            @event.RecipientId, @event.ConversationId);
    }

    // ── Channel initialization (lazy, thread-safe) ────────────────────────────

    private async Task EnsureChannelAsync()
    {
        if (_channel is not null && _channel.IsOpen) return;

        await _lock.WaitAsync();
        try
        {
            if (_channel is not null && _channel.IsOpen) return;

            var factory = new ConnectionFactory
            {
                HostName                 = _settings.Host,
                Port                     = _settings.Port,
                UserName                 = _settings.Username,
                Password                 = _settings.Password,
                VirtualHost              = _settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval  = TimeSpan.FromSeconds(10)
            };

            _connection = await factory.CreateConnectionAsync();
            _channel    = await _connection.CreateChannelAsync();

            // Declare exchange (idempotent — safe to call on every startup)
            await _channel.ExchangeDeclareAsync(
                exchange:   _settings.ExchangeName,
                type:       ExchangeType.Topic,
                durable:    true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ publisher channel initialized for Message Service");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _lock.Dispose();
    }
}
