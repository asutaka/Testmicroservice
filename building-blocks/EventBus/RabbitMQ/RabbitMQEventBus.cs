using System.Text;
using System.Text.Json;
using EventBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.RabbitMQ;

public sealed class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly string _exchangeName = "microservices_exchange";
    private readonly Dictionary<string, List<Type>> _handlers = new();
    private readonly Dictionary<string, Type> _eventTypes = new();

    public RabbitMQEventBus(
        IConnectionFactory connectionFactory,
        IServiceProvider serviceProvider,
        ILogger<RabbitMQEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
    }

    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        var eventName = integrationEvent.EventType;
        var message = JsonSerializer.Serialize(integrationEvent);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // persistent
        properties.MessageId = integrationEvent.Id.ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: eventName,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("Published integration event: {EventName} ({EventId})", eventName, integrationEvent.Id);
        return Task.CompletedTask;
    }

    public void Subscribe<T, TH>()
        where T : IIntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        var handlerType = typeof(TH);

        if (!_handlers.ContainsKey(eventName))
        {
            _handlers[eventName] = new List<Type>();
            _eventTypes[eventName] = typeof(T);
            StartConsuming(eventName);
        }

        _handlers[eventName].Add(handlerType);
        _logger.LogInformation("Subscribed {HandlerType} to {EventName}", handlerType.Name, eventName);
    }

    public void Unsubscribe<T, TH>()
        where T : IIntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = typeof(T).Name;
        var handlerType = typeof(TH);

        if (_handlers.TryGetValue(eventName, out var handlers))
            handlers.Remove(handlerType);
    }

    private void StartConsuming(string eventName)
    {
        var queueName = $"queue.{eventName}";
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, _exchangeName, eventName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                await ProcessEventAsync(eventName, message);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event: {EventName}", eventName);
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queueName, autoAck: false, consumer: consumer);
    }

    private async Task ProcessEventAsync(string eventName, string message)
    {
        if (!_handlers.TryGetValue(eventName, out var handlerTypes)) return;
        if (!_eventTypes.TryGetValue(eventName, out var eventType)) return;

        var integrationEvent = JsonSerializer.Deserialize(message, eventType);
        if (integrationEvent is null) return;

        using var scope = _serviceProvider.CreateScope();

        foreach (var handlerType in handlerTypes)
        {
            var handler = scope.ServiceProvider.GetService(handlerType);
            if (handler is null) continue;

            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod is null) continue;

            await (Task)handleMethod.Invoke(handler, [integrationEvent, CancellationToken.None])!;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
