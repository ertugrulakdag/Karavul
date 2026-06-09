using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Karavul.Host.Configuration;
using Karavul.Services;
using Microsoft.Extensions.Options;

namespace Karavul.Host.Workers;

public class MonitorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KaravulSettings _settings;
    private readonly ILogger<MonitorWorker> _logger;

    private readonly Dictionary<string, DateTime> _lastCheckTimes = new();
    private readonly SemaphoreSlim _semaphore = new(10, 10);

    public MonitorWorker(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        KaravulSettings settings,
        ILogger<MonitorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonitorWorker başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunChecksAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MonitorWorker döngüsünde hata oluştu.");
            }

            await Task.Delay(_settings.CheckWorkerIntervalMs, stoppingToken);
        }

        _logger.LogInformation("MonitorWorker durduruldu.");
    }

    private async Task RunChecksAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var monitorRepo = scope.ServiceProvider.GetRequiredService<IMonitorRepository>();
        var monitors = (await monitorRepo.GetActiveAsync()).ToList();

        if (!monitors.Any()) return;

        var now = DateTime.UtcNow;
        var tasks = monitors
            .Where(m => ShouldCheck(m.Id, m.CheckIntervalSeconds, now))
            .Select(m => CheckMonitorAsync(m, ct))
            .ToList();

        if (tasks.Any())
            await Task.WhenAll(tasks);
    }

    private bool ShouldCheck(string monitorId, int intervalSeconds, DateTime now)
    {
        if (!_lastCheckTimes.TryGetValue(monitorId, out var lastCheck))
            return true;
        return (now - lastCheck).TotalSeconds >= intervalSeconds;
    }

    private async Task CheckMonitorAsync(MonitorTarget monitor, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            _lastCheckTimes[monitor.Id] = DateTime.UtcNow;

            using var scope = _scopeFactory.CreateScope();
            var checkService = scope.ServiceProvider.GetRequiredService<MonitorCheckService>();
            var incidentService = scope.ServiceProvider.GetRequiredService<IncidentService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
            var incidentRepo = scope.ServiceProvider.GetRequiredService<IIncidentRepository>();
            var sslRepo = scope.ServiceProvider.GetRequiredService<ISslCheckRepository>();

            var httpClient = _httpClientFactory.CreateClient("MonitorClient");
            httpClient.Timeout = TimeSpan.FromSeconds(monitor.TimeoutSeconds);

            var check = await checkService.CheckHttpAsync(monitor, httpClient, ct);

            var openIncidentBefore = await incidentRepo.GetOpenByMonitorIdAsync(monitor.Id);
            var incident = await incidentService.ProcessCheckResultAsync(check, monitor);

            if (incident != null && incident.Status == Core.Enums.IncidentStatus.Open)
            {
                await notificationService.ProcessIncidentNotificationsAsync(incident, monitor, ct);
            }
            else if (openIncidentBefore != null && incident == null)
            {
                var resolvedIncident = await incidentRepo.GetByIdAsync(openIncidentBefore.Id);
                if (resolvedIncident != null)
                    await notificationService.ProcessRecoveryNotificationsAsync(resolvedIncident, monitor, ct);
            }

            if (monitor.CheckSsl && monitor.Url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                var latestSsl = await sslRepo.GetLatestByMonitorIdAsync(monitor.Id);
                if (latestSsl == null || (DateTime.UtcNow - latestSsl.CheckedAt).TotalHours >= 24)
                {
                    await checkService.CheckSslAsync(monitor, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monitor kontrol hatası: {MonitorName}", monitor.Name);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
