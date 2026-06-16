using System.Text.Json;
using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace JobVault.Infrastructure.Messaging.RabbitMQ;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private readonly string _exchangeName;
    private readonly string _deadLetterExchangeName;
    private readonly string _deadLetterQueueName;
    private readonly string _jobApplicationCreatedQueueName;
    private readonly string _jobApplicationUpdatedQueueName;
    private readonly string _sseNotificationsQueueName;
    private bool _disposed;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var connectionString = _configuration["RabbitMq:ConnectionString"];
        _exchangeName = _configuration["RabbitMq:ExchangeName"] ?? "job.applications";
        _deadLetterExchangeName = _configuration["RabbitMq:DeadLetterExchangeName"] ?? "job.applications.dead";
        _deadLetterQueueName = _configuration["RabbitMq:DeadLetterQueueName"] ?? "job.applications.dlq";
        _jobApplicationCreatedQueueName = _configuration["RabbitMq:JobApplicationCreatedQueueName"] ?? "job.applications.created";
        _jobApplicationUpdatedQueueName = _configuration["RabbitMq:JobApplicationUpdatedQueueName"] ?? "job.applications.updated";
        _sseNotificationsQueueName = _configuration["RabbitMq:SseNotificationsQueueName"] ?? "job.applications.notifications";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("RabbitMQ connection string not configured. Publisher will be disabled.");
            return;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString)
            };

            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // Declare dead letter exchange and queue
            _channel.ExchangeDeclareAsync(
                exchange: _deadLetterExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false).GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: _deadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false).GetAwaiter().GetResult();

            _channel.QueueBindAsync(
                queue: _deadLetterQueueName,
                exchange: _deadLetterExchangeName,
                routingKey: "").GetAwaiter().GetResult();

            // Declare main topic exchange
            _channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false).GetAwaiter().GetResult();

            // Declare queues with DLX configuration
            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", _deadLetterExchangeName }
            };

            _channel.QueueDeclareAsync(
                queue: _jobApplicationCreatedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs).GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: _jobApplicationUpdatedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs).GetAwaiter().GetResult();

            // Bind queues to exchange
            _channel.QueueBindAsync(
                queue: _jobApplicationCreatedQueueName,
                exchange: _exchangeName,
                routingKey: "job.application.created").GetAwaiter().GetResult();

            _channel.QueueBindAsync(
                queue: _jobApplicationUpdatedQueueName,
                exchange: _exchangeName,
                routingKey: "job.application.updated").GetAwaiter().GetResult();

            _logger.LogInformation("RabbitMQ publisher initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
        }
    }

    public async Task PublishJobApplicationEventAsync(JobApplicationEvent jobEvent)
    {
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ channel not initialized. Skipping publish.");
            return;
        }

        try
        {
            var routingKey = jobEvent.EventType.ToLowerInvariant() switch
            {
                "created" => "job.application.created",
                "updated" => "job.application.updated",
                _ => "job.application.unknown"
            };

            var json = JsonSerializer.Serialize(jobEvent);
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            // Publish a second copy with routing key "notification.new" so the SSE
            // notifications queue (bound to "notification.#") receives every event
            // independently of the Telegram queues
            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: "notification.new",
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published {EventType} event for {CompanyName} to RabbitMQ",
                jobEvent.EventType, jobEvent.CompanyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error publishing {EventType} event for {CompanyName} to RabbitMQ",
                jobEvent.EventType, jobEvent.CompanyName);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ publisher disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ publisher");
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}