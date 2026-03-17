using System;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using User.Application.DTOs;
using User.Application.Interfaces;

namespace User.Infrastructure.Messaging
{
    public class RabbitMQEventConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider; // sau này dùng để tạo 1 scope mới gọi về db tránh xung đột
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<RabbitMQEventConsumer> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQEventConsumer(
            IServiceProvider serviceProvider,
            IOptions<RabbitMQSettings> settings,
            ILogger<RabbitMQEventConsumer> logger
        )
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {   
            stoppingToken.Register(() => _logger.LogInformation("RabbitMQ Consumer is stopping")); // lệnh tắt server
            await InitializeRabbitMQ();

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var eventType = ea.BasicProperties.Type;

                    _logger.LogInformation("Received event {EventType} with routing key {RoutingKey}", eventType, ea.RoutingKey);
                
                    using var scope = _serviceProvider.CreateAsyncScope();
                    var UserProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();

                    await HandleEventAsync(eventType, message, UserProfileService);

                    //Acknowledge message (xác nhận xử lý thành công)
                    await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                    _logger.LogInformation("Successfully processed event {EventType}", eventType);
                    
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    //message sẽ ko dc xử lý lại
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    // await _channel.BasicAckAsync(ea.DeliveryTag, multiple:false);
                }
            };

            //consume messages
            await _channel!.BasicConsumeAsync(
                queue: _settings.QueueName,
                autoAck: false, // chờ ack, nack để quyết định xóa message hay không
                consumer: consumer
            );
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
            _logger.LogInformation("RabbitMQ Consumer started successfully");
            
        }
        private async Task InitializeRabbitMQ()
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
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10) // thời gian reconnect khi mất kết nối
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();

                //Declare exchange (dựa vào type để đưa message vào queue nào)
                await _channel.ExchangeDeclareAsync(
                    exchange: _settings.ExchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                );

                await _channel.QueueDeclareAsync(
                    queue: _settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null  
                );

                // Routing key
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "user.userregistered");
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "user.usergoogleregistered");
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "user.userprofileupdated");
                await _channel.QueueBindAsync(_settings.QueueName, _settings.ExchangeName, "user.userdeleted");
                
                //số message tối đa handle 1 lần
                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: true);
                _logger.LogInformation("RabbitMQ initialized successfully");

            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ");
                throw;
            }
        }

        private async Task HandleEventAsync(
            string eventType,
            string message,
            IUserProfileService service
        )
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // không phân biệt chữ hoa, thường
            };

            switch (eventType)
            {
                case "UserRegisteredEvent":
                    var registeredEvent = JsonSerializer.Deserialize<UserRegisteredEventDto>(message, options);
                    if (registeredEvent != null)
                    {
                        await service.CreateProfileFromIdentityAsync(
                            registeredEvent.UserId,
                            registeredEvent.Email,
                            registeredEvent.FirstName,
                            registeredEvent.LastName,
                            registeredEvent.DateOfBirth,
                            registeredEvent.Gender
                        );
                    };
                    break;
                case "UserGoogleRegisteredEvent":
                    {
                        var googleEvent = JsonSerializer.Deserialize<UserGoogleRegisteredEventDto>(message, options);
                        if (googleEvent != null)
                        {
                            await service.CreateProfileFromIdentityAsync(
                                googleEvent.UserId,
                                googleEvent.Email,
                                googleEvent.FirstName,
                                googleEvent.LastName,
                                DateTime.MinValue,
                                null
                            );
                        }
                        break;
                    }
                case "UserProfileUpdatedEvent":
                var updatedEvent = JsonSerializer.Deserialize<UserProfileUpdatedEventDto>(message, options);
                if (updatedEvent != null)
                {
                    await service.UpdateProfileFromIdentityAsync(
                        updatedEvent.UserId,
                        updatedEvent.FirstName,
                        updatedEvent.LastName,
                        updatedEvent.Gender
                    );
                }
                break;

            case "UserDeletedEvent":
                var deletedEvent = JsonSerializer.Deserialize<UserDeletedEventDto>(message, options);
                if (deletedEvent != null)
                {
                    await service.SoftDeleteProfileAsync(
                        deletedEvent.UserId,
                        deletedEvent.Reason
                    );
                }
                break;

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
            _logger.LogInformation("RabbitMQ Consumer disposed");
            base.Dispose();
        }
    }
}
    