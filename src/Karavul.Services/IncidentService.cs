using Karavul.Core.Entities;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Karavul.Services;

public class IncidentService
{
    private readonly IIncidentRepository _incidentRepo;
    private readonly IMonitorRepository _monitorRepo;
    private readonly ILogger<IncidentService> _logger;

    public IncidentService(
        IIncidentRepository incidentRepo,
        IMonitorRepository monitorRepo,
        ILogger<IncidentService> logger)
    {
        _incidentRepo = incidentRepo;
        _monitorRepo = monitorRepo;
        _logger = logger;
    }

    /// <summary>
    /// Check sonucuna göre incident açar veya kapatır.
    /// </summary>
    public async Task<Incident?> ProcessCheckResultAsync(MonitorCheck check, MonitorTarget monitor)
    {
        var openIncident = await _incidentRepo.GetOpenByMonitorIdAsync(monitor.Id);

        if (!check.IsSuccess)
        {
            var newStatus = check.CheckResultType == CheckResultType.ResponseTimeTooHigh
                ? MonitorStatus.Warning
                : MonitorStatus.Down;

            await _monitorRepo.UpdateStatusAsync(monitor.Id, newStatus,
                check.StatusCode, check.ResponseTimeMs, check.ErrorMessage);

            if (openIncident == null)
            {
                var incident = new Incident
                {
                    MonitorId = monitor.Id,
                    StartedAt = DateTime.UtcNow,
                    Status = IncidentStatus.Open,
                    Reason = check.CheckResultType.ToString(),
                    LastErrorMessage = check.ErrorMessage,
                    NotificationCount = 0
                };

                await _incidentRepo.CreateAsync(incident);
                _logger.LogWarning("Yeni incident açıldı: Monitor={MonitorName}, Reason={Reason}",
                    monitor.Name, incident.Reason);

                return incident;
            }
            else
            {
                openIncident.LastErrorMessage = check.ErrorMessage;
                await _incidentRepo.UpdateAsync(openIncident);
                return openIncident;
            }
        }
        else
        {
            await _monitorRepo.UpdateStatusAsync(monitor.Id, MonitorStatus.Up,
                check.StatusCode, check.ResponseTimeMs, null);

            if (openIncident != null)
            {
                await _incidentRepo.ResolveAsync(openIncident.Id, DateTime.UtcNow);
                _logger.LogInformation("Incident çözüldü: Monitor={MonitorName}, IncidentId={IncidentId}",
                    monitor.Name, openIncident.Id);
            }

            return null;
        }
    }
}
