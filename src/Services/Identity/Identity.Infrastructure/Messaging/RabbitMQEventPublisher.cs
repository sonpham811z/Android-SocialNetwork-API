using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Identity.Application.Interfaces;
using Identity.Domain.Events;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Identity.Infrastructure.Messaging
{
    public class RabbitMQEventPublisher : IEventPublisher, IDisposable
    {
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQEventPublisher> _logger;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly SemaphoreSlim _channelLock = new SemaphoreSlim(1, 1);

        public RabbitMQEventPublisher(
            IOptions<RabbitMQSettings> settings,
            ILogger<RabbitMQEventPublisher> logger
        )
        {
            _settings = settings.Value;
            _logger = logger;
            InitializeRabbitMQ();

        }
    

    private async Task InitializeRabbitMQ()
        {
            try
            {
                var factory = new ConnectionFactory()
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
                _channel = await  _connection.CreateChannelAsync();

                await _channel.ExchangeDeclareAsync(
                    exchange: _settings.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                );

                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
                throw;
            }
        }

        public async Task PublishAsync<T>(T @event) where T : UserEvent
        {
            if (_channel == null || !_channel.IsOpen)
            {
                 _logger.LogWarning("RabbitMQ channel is closed. Attempting to reconnect...");
                await InitializeRabbitMQ();
            }

            try
            {
                var eventType = @event.GetType().Name;

                //Routing key: user.userr=deleted, user.userregisterd,..
                var routingKey = $"user.{eventType.Replace("Event", "").ToLower()}";

                var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var body = Encoding.UTF8.GetBytes(message);

                var properties = new BasicProperties();
                properties.Persistent = true; // message lưu vào disk
                properties.ContentType = "application/json";
                properties.Type = eventType;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

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
                };

                _logger.LogInformation(
                    "Published event {EventType} with routing key {RoutingKey} for user {UserId}",
                    eventType, routingKey, @event.UserId);

        } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event {EventType}", typeof(T).Name);
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
                _logger.LogInformation("RabbitMQ connection disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing ReabbitMQ connection");
            }
        }   
    }
}
