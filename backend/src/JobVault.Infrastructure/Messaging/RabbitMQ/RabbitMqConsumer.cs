using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace JobVault.Infrastructure.Messaging.RabbitMQ;

public class RabbitMqConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationHub _notificationHub;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        INotificationHub notificationHub,
        ILogger<RabbitMqConsumer> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _notificationHub = notificationHub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration["RabbitMq:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("RabbitMQ connection string not configured. Consumer will not start.");
            return;
        }

        try
        {
            await InitializeRabbitMqAsync(connectionString);
            await ConsumeMessagesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in RabbitMQ consumer");
        }
    }

    private async Task InitializeRabbitMqAsync(string connectionString)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        var exchangeName   = _configuration["RabbitMq:ExchangeName"]                   ?? "job.applications";
        var dlxName        = _configuration["RabbitMq:DeadLetterExchangeName"]         ?? "job.applications.dead";
        var dlqName        = _configuration["RabbitMq:DeadLetterQueueName"]            ?? "job.applications.dlq";
        var createdQueue   = _configuration["RabbitMq:JobApplicationCreatedQueueName"] ?? "job.applications.created";
        var updatedQueue   = _configuration["RabbitMq:JobApplicationUpdatedQueueName"] ?? "job.applications.updated";

        // Dead-letter exchange + queue
        await _channel.ExchangeDeclareAsync(exchange: dlxName, type: ExchangeType.Direct, durable: true, autoDelete: false);
        await _channel.QueueDeclareAsync(queue: dlqName, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(queue: dlqName, exchange: dlxName, routingKey: "");

        // Main topic exchange
        await _channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false);

        // Created / updated queues with DLX
        var queueArgs = new Dictionary<string, object?> { { "x-dead-letter-exchange", dlxName } };
        await _channel.QueueDeclareAsync(queue: createdQueue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        await _channel.QueueBindAsync(queue: createdQueue, exchange: exchangeName, routingKey: "job.application.created");

        await _channel.QueueDeclareAsync(queue: updatedQueue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        await _channel.QueueBindAsync(queue: updatedQueue, exchange: exchangeName, routingKey: "job.application.updated");

        _logger.LogInformation("RabbitMQ consumer initialized on queues: {Created}, {Updated}", createdQueue, updatedQueue);
    }

    private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        if (_channel == null) return;

        var createdQueue = _configuration["RabbitMq:JobApplicationCreatedQueueName"] ?? "job.applications.created";
        var updatedQueue = _configuration["RabbitMq:JobApplicationUpdatedQueueName"] ?? "job.applications.updated";

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                var jobEvent = JsonSerializer.Deserialize<JobApplicationEvent>(message, _jsonOptions);

                if (jobEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize message, rejecting");
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                if (string.IsNullOrEmpty(jobEvent.EventType) && string.IsNullOrEmpty(jobEvent.CompanyName))
                {
                    _logger.LogWarning("Deserialized JobApplicationEvent has empty EventType and CompanyName — possible JSON field name mismatch. Raw: {Message}", message);
                    await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await ProcessJobEventAsync(jobEvent);
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(queue: createdQueue, autoAck: false, consumer: consumer);
        await _channel.BasicConsumeAsync(queue: updatedQueue, autoAck: false, consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessJobEventAsync(JobApplicationEvent jobEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();

        _logger.LogInformation("Processing {EventType} event for {CompanyName}",
            jobEvent.EventType, jobEvent.CompanyName);

        await telegramService.SendNotificationAsync(jobEvent);

        var notification = SseNotificationConsumer.BuildNotification(jobEvent);
        await _notificationHub.BroadcastAsync(notification);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQ consumer");
        _channel?.Dispose();
        _connection?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
