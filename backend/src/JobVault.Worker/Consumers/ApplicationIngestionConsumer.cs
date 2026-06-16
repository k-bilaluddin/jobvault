using JobVault.Application.Interfaces;
using JobVault.Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace JobVault.Worker.Consumers;

public class ApplicationIngestionConsumer : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const int MaxRetries = 3;

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApplicationIngestionConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public ApplicationIngestionConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<ApplicationIngestionConsumer> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _configuration["RabbitMq:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogWarning("RabbitMQ connection string not configured. ApplicationIngestionConsumer will not start.");
            return;
        }

        try
        {
            await InitializeAsync(connectionString);
            await ConsumeAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ApplicationIngestionConsumer");
        }
    }

    private async Task InitializeAsync(string connectionString)
    {
        var factory = new ConnectionFactory { Uri = new Uri(connectionString) };
        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        var queueName = _configuration["RabbitMq:JobApplicationReceivedQueueName"] ?? "job.applications.received";
        _logger.LogInformation("ApplicationIngestionConsumer initialized on queue: {Queue}", queueName);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        if (_channel == null) return;

        var queueName = _configuration["RabbitMq:JobApplicationReceivedQueueName"] ?? "job.applications.received";
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            JobApplicationEvent? jobEvent = null;

            try
            {
                jobEvent = JsonSerializer.Deserialize<JobApplicationEvent>(message, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize ingestion message, dead-lettering");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            if (jobEvent?.ApplicationId == null)
            {
                _logger.LogWarning("Missing ApplicationId in ingestion message, dead-lettering. Raw: {Msg}", message);
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            var success = await ProcessWithRetryAsync(jobEvent.ApplicationId, stoppingToken);

            if (success)
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            else
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        };

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
    }

    private async Task<bool> ProcessWithRetryAsync(string applicationId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IApplicationProcessorService>();
                await processor.ProcessAsync(applicationId, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing attempt {Attempt}/{Max} failed for applicationId={Id}",
                    attempt, MaxRetries, applicationId);

                if (attempt < MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s
                    _logger.LogInformation("Retrying in {Delay}s...", delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        _logger.LogError("All {Max} attempts exhausted for applicationId={Id}; dead-lettering", MaxRetries, applicationId);
        return false;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping ApplicationIngestionConsumer");
        _channel?.Dispose();
        _connection?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
