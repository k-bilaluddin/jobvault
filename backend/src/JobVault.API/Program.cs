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

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HTTP client factory
builder.Services.AddHttpClient();

// CORS — env var CORS_ALLOWED_ORIGINS takes priority, then Cors:AllowedOrigins from config
var rawOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
    ?? builder.Configuration["Cors:AllowedOrigins"];

if (string.IsNullOrWhiteSpace(rawOrigins))
    throw new InvalidOperationException(
        "CORS allowed origins must be configured via CORS_ALLOWED_ORIGINS env var or Cors:AllowedOrigins config key.");

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
