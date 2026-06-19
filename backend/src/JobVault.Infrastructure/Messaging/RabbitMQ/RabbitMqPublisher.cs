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
    private readonly string? _connectionString;
    private readonly string _exchangeName;
    private readonly string _deadLetterExchangeName;
    private readonly string _deadLetterQueueName;
    private readonly string _jobApplicationCreatedQueueName;
    private readonly string _jobApplicationUpdatedQueueName;
    private readonly string _sseNotificationsQueueName;
    private readonly string _jobApplicationReceivedQueueName;

    // Lazily initialized on first publish; null if RabbitMQ is not configured.
    private readonly Lazy<Task<IChannel?>> _channelLazy;
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _connectionString = _configuration["RabbitMq:ConnectionString"];
        _exchangeName = _configuration["RabbitMq:ExchangeName"] ?? "job.applications";
        _deadLetterExchangeName = _configuration["RabbitMq:DeadLetterExchangeName"] ?? "job.applications.dead";
        _deadLetterQueueName = _configuration["RabbitMq:DeadLetterQueueName"] ?? "job.applications.dlq";
        _jobApplicationCreatedQueueName = _configuration["RabbitMq:JobApplicationCreatedQueueName"] ?? "job.applications.created";
        _jobApplicationUpdatedQueueName = _configuration["RabbitMq:JobApplicationUpdatedQueueName"] ?? "job.applications.updated";
        _sseNotificationsQueueName = _configuration["RabbitMq:SseNotificationsQueueName"] ?? "job.applications.notifications";
        _jobApplicationReceivedQueueName = _configuration["RabbitMq:JobApplicationReceivedQueueName"] ?? "job.applications.received";

        _channelLazy = new Lazy<Task<IChannel?>>(InitializeChannelAsync, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private async Task<IChannel?> InitializeChannelAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogWarning("RabbitMQ connection string not configured. Publisher will be disabled.");
            return null;
        }

        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };

            _connection = await factory.CreateConnectionAsync();
            var channel = await _connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: _deadLetterExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            await channel.QueueDeclareAsync(
                queue: _deadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await channel.QueueBindAsync(
                queue: _deadLetterQueueName,
                exchange: _deadLetterExchangeName,
                routingKey: "");

            await channel.ExchangeDeclareAsync(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", _deadLetterExchangeName }
            };

            await channel.QueueDeclareAsync(
                queue: _jobApplicationCreatedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            await channel.QueueDeclareAsync(
                queue: _jobApplicationUpdatedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            await channel.QueueBindAsync(
                queue: _jobApplicationCreatedQueueName,
                exchange: _exchangeName,
                routingKey: "job.application.created");

            await channel.QueueBindAsync(
                queue: _jobApplicationUpdatedQueueName,
                exchange: _exchangeName,
                routingKey: "job.application.updated");

            await channel.QueueDeclareAsync(
                queue: _jobApplicationReceivedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            await channel.QueueBindAsync(
                queue: _jobApplicationReceivedQueueName,
                exchange: _exchangeName,
                routingKey: "job.application.received");

            _logger.LogInformation("RabbitMQ publisher initialized successfully");
            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
            return null;
        }
    }

    public async Task PublishJobApplicationEventAsync(JobApplicationEvent jobEvent)
    {
        var channel = await _channelLazy.Value;

        if (channel == null)
        {
            _logger.LogWarning("RabbitMQ channel not available. Skipping publish.");
            return;
        }

        try
        {
            var routingKey = jobEvent.EventType.ToLowerInvariant() switch
            {
                "created" => "job.application.created",
                "updated" => "job.application.updated",
                "received" => "job.application.received",
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

            await channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            // Fan-out to SSE queue for created/updated events only.
            // "received" events are consumed only by the Worker's ingestion consumer
            // and should not surface as in-app notifications until the Worker completes.
            if (routingKey != "job.application.received")
            {
                await channel.BasicPublishAsync(
                    exchange: _exchangeName,
                    routingKey: "notification.new",
                    mandatory: false,
                    basicProperties: properties,
                    body: body);
            }

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
            return;

        try
        {
            if (_channelLazy.IsValueCreated && _channelLazy.Value.IsCompletedSuccessfully)
                _channelLazy.Value.Result?.Dispose();

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
