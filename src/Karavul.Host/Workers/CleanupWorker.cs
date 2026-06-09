using Karavul.Core.Interfaces;
using Karavul.Host.Configuration;

namespace Karavul.Host.Workers;

public class CleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KaravulSettings _settings;
    private readonly ILogger<CleanupWorker> _logger;

    public CleanupWorker(
        IServiceScopeFactory scopeFactory,
        KaravulSettings settings,
        ILogger<CleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CleanupWorker başlatıldı. Retention: {Days} gün", _settings.RetentionDays);

        // İlk cleanup başlangıçta çalıştır
        await RunCleanupAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(_settings.CleanupIntervalHours), stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
                await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken ct)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-_settings.RetentionDays);
            _logger.LogInformation("Temizlik başlıyor. Cutoff: {Cutoff:yyyy-MM-dd}", cutoff);

            using var scope = _scopeFactory.CreateScope();
            var checkRepo = scope.ServiceProvider.GetRequiredService<IMonitorCheckRepository>();
            var notifRepo = scope.ServiceProvider.GetRequiredService<INotificationLogRepository>();
            var sslRepo = scope.ServiceProvider.GetRequiredService<ISslCheckRepository>();

            await checkRepo.DeleteOlderThanAsync(cutoff);
            await notifRepo.DeleteOlderThanAsync(cutoff);
            await sslRepo.DeleteOlderThanAsync(cutoff);

            _logger.LogInformation("Temizlik tamamlandı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Temizlik sırasında hata oluştu.");
        }
    }
}
