using JobVault.Application.Interfaces;
using JobVault.Application.Services;
using JobVault.Infrastructure.GitHub;
using JobVault.Infrastructure.Messaging.RabbitMQ;
using JobVault.Infrastructure.Notifications;
using JobVault.Infrastructure.Notifications.Telegram;
using JobVault.Infrastructure.Persistence.MongoDB;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HTTP client factory
builder.Services.AddHttpClient();

// CORS — required for EventSource (SSE) from the Vue frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register application services
builder.Services.AddSingleton<IJobApplicationRepository, MongoDbService>();
builder.Services.AddSingleton<IGitHubFileService, GitHubFileService>();
builder.Services.AddSingleton<IMarkdownParserService, MarkdownParserService>();
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<ITelegramNotificationService, TelegramNotificationService>();
builder.Services.AddScoped<IWebhookHandler, WebhookHandler>();
builder.Services.AddScoped<IFileIngestService, FileIngestService>();

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
app.UseAuthorization();
app.MapControllers();

app.Run();
