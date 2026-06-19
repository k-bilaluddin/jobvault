using System.Diagnostics;
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
        // Required for TracePropagationHandler to inject valid W3C traceparent headers.
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        var builder = Host.CreateApplicationBuilder(args);

        // Map SCREAMING_SNAKE_CASE env vars to .NET config paths (highest priority — overrides appsettings)
        static string? E(string n) => Environment.GetEnvironmentVariable(n);
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Auth:Email"]                               = E("AUTH_EMAIL"),
                ["Auth:PasswordHash"]                        = E("AUTH_PASSWORD_HASH"),
                ["Auth:JwtSecret"]                           = E("AUTH_JWT_SECRET"),
                ["Auth:TokenExpiryDays"]                     = E("AUTH_TOKEN_EXPIRY_DAYS"),
                ["Demo:Email"]                               = E("DEMO_EMAIL"),
                ["Demo:PasswordHash"]                        = E("DEMO_PASSWORD_HASH"),
                ["Cors:AllowedOrigins"]                      = E("CORS_ALLOWED_ORIGINS"),
                ["MongoDb:ConnectionString"]                 = E("MONGODB_CONNECTION_STRING"),
                ["MongoDb:DatabaseName"]                     = E("MONGODB_DATABASE_NAME"),
                ["MongoDb:JobApplicationsCollectionName"]    = E("MONGODB_JOB_APPLICATIONS_COLLECTION"),
                ["MongoDb:NotificationsCollectionName"]      = E("MONGODB_NOTIFICATIONS_COLLECTION"),
                ["RabbitMq:ConnectionString"]                = E("RABBITMQ_CONNECTION_STRING"),
                ["RabbitMq:ExchangeName"]                    = E("RABBITMQ_EXCHANGE_NAME"),
                ["RabbitMq:DeadLetterExchangeName"]          = E("RABBITMQ_DEAD_LETTER_EXCHANGE_NAME"),
                ["RabbitMq:DeadLetterQueueName"]             = E("RABBITMQ_DEAD_LETTER_QUEUE_NAME"),
                ["RabbitMq:JobApplicationCreatedQueueName"]  = E("RABBITMQ_JOB_APPLICATION_CREATED_QUEUE"),
                ["RabbitMq:JobApplicationUpdatedQueueName"]  = E("RABBITMQ_JOB_APPLICATION_UPDATED_QUEUE"),
                ["RabbitMq:JobApplicationReceivedQueueName"] = E("RABBITMQ_JOB_APPLICATION_RECEIVED_QUEUE"),
                ["RabbitMq:SseNotificationsQueueName"]       = E("RABBITMQ_SSE_NOTIFICATIONS_QUEUE"),
                ["Telegram:BotToken"]                        = E("TELEGRAM_BOT_TOKEN"),
                ["Telegram:ChatId"]                          = E("TELEGRAM_CHAT_ID"),
                ["GitHub:Token"]                             = E("APP_GH_TOKEN"),
                ["GitHub:Owner"]                             = E("APP_GH_OWNER"),
                ["GitHub:Repository"]                        = E("APP_GH_REPOSITORY"),
                ["GitHub:Branch"]                            = E("APP_GH_BRANCH"),
                ["GitHub:CvFileName"]                        = E("APP_GH_CV_FILE_NAME"),
                ["GitHub:CoverLetterFileName"]               = E("APP_GH_COVER_LETTER_FILE_NAME"),
                ["DocumentGeneration:BaseUrl"]               = E("DOCUMENT_GENERATION_BASE_URL"),
                ["LibreOffice:ExecutablePath"]               = E("LIBREOFFICE_EXECUTABLE_PATH"),
            }
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value));

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
