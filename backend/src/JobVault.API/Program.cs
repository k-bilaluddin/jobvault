using System.Text;
using JobVault.Application.Interfaces;
using JobVault.Application.Services;
using JobVault.Infrastructure.GitHub;
using JobVault.Infrastructure.Messaging.RabbitMQ;
using JobVault.Infrastructure.Notifications;
using JobVault.Infrastructure.Notifications.Telegram;
using JobVault.Infrastructure.Persistence.MongoDB;
using JobVault.Infrastructure.Processing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
        ["Vault:RootDir"]                            = E("APP_VAULT_ROOT_DIR"),
    }
    .Where(kv => kv.Value is not null)
    .ToDictionary(kv => kv.Key, kv => kv.Value));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HTTP client factory
builder.Services.AddHttpClient();

// CORS — set via CORS_ALLOWED_ORIGINS env var or Cors:AllowedOrigins config key
var rawOrigins = builder.Configuration["Cors:AllowedOrigins"];

if (string.IsNullOrWhiteSpace(rawOrigins))
    throw new InvalidOperationException(
        "CORS allowed origins must be configured via CORS_ALLOWED_ORIGINS env var or Cors:AllowedOrigins config key. See docs/env.md.");

var allowedOrigins = rawOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// JWT authentication — fail fast if secret is missing or too short
var jwtSecret = builder.Configuration["Auth:JwtSecret"]
    ?? throw new InvalidOperationException("Auth:JwtSecret is not configured.");

if (jwtSecret.Length < 32)
    throw new InvalidOperationException("Auth:JwtSecret must be at least 32 characters.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer           = false,
            ValidateAudience         = false,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Register application services
builder.Services.AddSingleton<IJobApplicationRepository, MongoDbService>();
builder.Services.AddSingleton<IGitHubFileService, GitHubFileService>();
builder.Services.AddSingleton<IMarkdownParserService, MarkdownParserService>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<ITelegramNotificationService, TelegramNotificationService>();
builder.Services.AddScoped<IWebhookHandler, WebhookHandler>();
builder.Services.AddScoped<IFileIngestService, FileIngestService>();
builder.Services.AddScoped<IApplicationIngestionService, ApplicationIngestionService>();

// Notification services
builder.Services.AddSingleton<INotificationHub, NotificationHub>();
builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();
builder.Services.AddHostedService<SseNotificationConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
