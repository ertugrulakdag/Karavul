using Karavul.Core.Entities;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;
using Karavul.Data.Repositories;
using Karavul.Host.Configuration;
using Karavul.Services;
using Karavul.Services.Notifications;
using Microsoft.Extensions.Options;

namespace Karavul.Host.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKaravulServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Settings
        services.Configure<KaravulSettings>(configuration.GetSection("Karavul"));
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<KaravulSettings>>().Value);

        // Database
        services.AddSingleton<DbConnectionFactory>(sp =>
        {
            var settings = sp.GetRequiredService<KaravulSettings>();
            var dir = Path.GetDirectoryName(settings.DatabasePath)!;
            Directory.CreateDirectory(dir);
            return new DbConnectionFactory($"Data Source={settings.DatabasePath}");
        });

        services.AddSingleton<SchemaInitializer>();

        // Repositories
        services.AddScoped<IMonitorRepository, MonitorRepository>();
        services.AddScoped<IMonitorCheckRepository, MonitorCheckRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IContactGroupRepository, ContactGroupRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<ISslCheckRepository, SslCheckRepository>();
        services.AddScoped<IDirectoryContactRepository, DirectoryContactRepository>();

        // Services
        services.AddScoped<AuthService>();
        services.AddScoped<MonitorCheckService>();
        services.AddScoped<IncidentService>();
        services.AddScoped<NotificationService>();

        // Notification senders
        services.AddScoped<INotificationSender, EmailNotificationSender>();
        services.AddScoped<INotificationSender, SmsNotificationSender>();
        services.AddScoped<INotificationSender, PushNotificationSender>();
        services.AddScoped<INotificationSender>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<TelegramNotificationSender>>();
            var settings = sp.GetRequiredService<KaravulSettings>();
            var botToken = settings.Telegram?.BotToken ?? string.Empty;
            return new TelegramNotificationSender(logger, botToken);
        });

        // HttpClient factory for uptime checks
        services.AddHttpClient("MonitorClient")
            .ConfigureHttpClient(c =>
            {
                c.DefaultRequestHeaders.Add("User-Agent", "Karavul-StatusMonitor/1.0");
            });

        return services;
    }
}
