using JobVault.Application.Interfaces;
using JobVault.Infrastructure.Messaging.RabbitMQ;
using JobVault.Infrastructure.Notifications.Telegram;

namespace JobVault.Worker;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();
        builder.Services.AddHostedService<RabbitMqConsumer>();

        var host = builder.Build();
        host.Run();
    }
}