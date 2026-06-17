using JobVault.Application.Interfaces;
using JobVault.Infrastructure.Generation;
using JobVault.Infrastructure.GitHub;
using JobVault.Infrastructure.Messaging.RabbitMQ;
using JobVault.Infrastructure.Notifications;
using JobVault.Infrastructure.Notifications.Telegram;
using JobVault.Infrastructure.Persistence.MongoDB;
using JobVault.Infrastructure.Processing;

namespace JobVault.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // HTTP client factory — required by FileIngestService and GitHubFileService
        builder.Services.AddHttpClient();

        // Document generation service — typed client with W3C trace propagation
        builder.Services.AddTransient<TracePropagationHandler>();
        builder.Services.AddHttpClient<IDocumentGenerationClient, DocumentGenerationClient>(client =>
        {
            var baseUrl = builder.Configuration["DocumentGeneration:BaseUrl"]
                          ?? "http://jobvault-generation-service:3000";
            client.BaseAddress = new Uri(baseUrl);
        })
        .AddHttpMessageHandler<TracePropagationHandler>();

        // Persistence
        builder.Services.AddSingleton<IJobApplicationRepository, MongoDbService>();

        // GitHub
        builder.Services.AddSingleton<IGitHubFileService, GitHubFileService>();
        builder.Services.AddScoped<IFileIngestService, FileIngestService>();

        // Messaging
        builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

        // Notifications
        builder.Services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
        builder.Services.AddSingleton<INotificationHub, NotificationHub>();

        // Processing — scoped so each consumer message gets an isolated instance
        builder.Services.AddScoped<IApplicationProcessorService, ApplicationProcessorService>();

        // Hosted consumers
        builder.Services.AddHostedService<RabbitMqConsumer>();           // Telegram + SSE fan-out
        builder.Services.AddHostedService<ApplicationIngestionConsumer>(); // Async ingestion pipeline

        var host = builder.Build();
        host.Run();
    }
}
