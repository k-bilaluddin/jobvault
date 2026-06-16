using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using JobVault.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace JobVault.Infrastructure.Messaging.RabbitMQ;

public class SseNotificationConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationHub _notificationHub;
    private readonly ILogger<SseNotificationConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public SseNotificationConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        INotificationHub notificationHub,
        ILogger<SseNotificationConsumer> logger)
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
            _logger.LogWarning("RabbitMQ connection string not configured. SSE consumer will not start.");
            return;
        }

        try
        {
            await InitializeRabbitMqAsync(connectionString);
            await ConsumeMessagesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SSE notification consumer");
        }
    }

    private async Task InitializeRabbitMqAsync(string connectionString)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        var exchangeName = _configuration["RabbitMq:ExchangeName"] ?? "job.applications";
        var deadLetterExchangeName = _configuration["RabbitMq:DeadLetterExchangeName"] ?? "job.applications.dead";
        var queueName = _configuration["RabbitMq:SseNotificationsQueueName"] ?? "job.applications.notifications";

        // Declare queue with same durable/exclusive/autoDelete/DLX args as the existing queues
        var queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", deadLetterExchangeName }
        };

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);

        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: "notification.#");

        _logger.LogInformation("SSE notification consumer initialized on queue: {QueueName}", queueName);
    }

    private async Task ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        if (_channel == null) return;

        var queueName = _configuration["RabbitMq:SseNotificationsQueueName"] ?? "job.applications.notifications";
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                var jobEvent = JsonSerializer.Deserialize<JobApplicationEvent>(message, _jsonOptions);

                if (jobEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize SSE notification message, rejecting");
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
                _logger.LogError(ex, "Error processing SSE notification message");
                await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessJobEventAsync(JobApplicationEvent jobEvent)
    {
        var notification = BuildNotification(jobEvent);

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        await repository.SaveAsync(notification);

        await _notificationHub.BroadcastAsync(notification);

        _logger.LogInformation("Processed SSE notification for {CompanyName} ({EventType})",
            jobEvent.CompanyName, jobEvent.EventType);
    }

    internal static AppNotification BuildNotification(JobApplicationEvent jobEvent)
    {
        var (type, title, body) = jobEvent.EventType.ToLowerInvariant() switch
        {
            "created" => (
                "new_application",
                $"New application: {jobEvent.CompanyName}",
                $"{jobEvent.Recommendation} · {jobEvent.MatchScore}% match · {jobEvent.Status}"
            ),
            "updated" => (
                "stage_changed",
                $"Application updated: {jobEvent.CompanyName}",
                $"{jobEvent.Recommendation} · {jobEvent.MatchScore}% match · {jobEvent.Status}"
            ),
            _ => (
                "sync_completed",
                $"Event for {jobEvent.CompanyName}",
                jobEvent.Status
            )
        };

        return new AppNotification
        {
            Id = Guid.NewGuid(),
            Type = type,
            Title = title,
            Body = body,
            CompanyName = jobEvent.CompanyName,
            CompanySlug = SlugifyCompanyName(jobEvent.CompanyName),
            OccurredAt = jobEvent.Timestamp == default ? DateTime.UtcNow : jobEvent.Timestamp,
            Read = false
        };
    }

    private static string SlugifyCompanyName(string name) =>
        name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("'", "")
            .Trim('-');

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping SSE notification consumer");
        _channel?.Dispose();
        _connection?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
