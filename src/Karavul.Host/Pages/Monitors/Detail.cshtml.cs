using Karavul.Core.DTOs;
using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Monitors;

public class DetailModel : PageModel
{
    private readonly IMonitorRepository _monitorRepo;
    private readonly IMonitorCheckRepository _checkRepo;
    private readonly IIncidentRepository _incidentRepo;
    private readonly ISslCheckRepository _sslRepo;
    private readonly IContactGroupRepository _groupRepo;
    private readonly Karavul.Services.MonitorCheckService _checkService;
    private readonly Karavul.Services.IncidentService _incidentService;
    private readonly Karavul.Services.NotificationService _notificationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Karavul.Host.Services.RealtimeGraphService _realtimeGraphService;

    public DetailModel(
        IMonitorRepository monitorRepo,
        IMonitorCheckRepository checkRepo,
        IIncidentRepository incidentRepo,
        ISslCheckRepository sslRepo,
        IContactGroupRepository groupRepo,
        Karavul.Services.MonitorCheckService checkService,
        Karavul.Services.IncidentService incidentService,
        Karavul.Services.NotificationService notificationService,
        IHttpClientFactory httpClientFactory,
        Karavul.Host.Services.RealtimeGraphService realtimeGraphService)
    {
        _monitorRepo = monitorRepo;
        _checkRepo = checkRepo;
        _incidentRepo = incidentRepo;
        _sslRepo = sslRepo;
        _groupRepo = groupRepo;
        _checkService = checkService;
        _incidentService = incidentService;
        _notificationService = notificationService;
        _httpClientFactory = httpClientFactory;
        _realtimeGraphService = realtimeGraphService;
    }

    public MonitorTarget Monitor { get; set; } = null!;
    public MonitorDetailDto Detail { get; set; } = new();

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalChecks { get; set; }
    public Karavul.Core.DTOs.PaginationModel Pagination { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id, int p = 1)
    {
        var monitor = await _monitorRepo.GetByIdAsync(id);
        if (monitor == null) return NotFound();

        Monitor = monitor;
        var now = DateTime.UtcNow;

        var uptime24h = await _checkRepo.GetUptimePercentageAsync(id, now.AddHours(-24));
        var uptime7d = await _checkRepo.GetUptimePercentageAsync(id, now.AddDays(-7));
        var uptime30d = await _checkRepo.GetUptimePercentageAsync(id, now.AddDays(-30));
        var avgResp = await _checkRepo.GetAverageResponseTimeAsync(id, now.AddDays(-7));
        
        CurrentPage = p;
        var pagedChecks = await _checkRepo.GetPagedByMonitorIdAsync(id, p, 10);
        TotalChecks = pagedChecks.TotalCount;
        TotalPages = (int)Math.Ceiling(TotalChecks / 10.0);

        Pagination = new Karavul.Core.DTOs.PaginationModel
        {
            CurrentPage = p,
            TotalRecords = pagedChecks.TotalCount,
            TotalPages = (int)Math.Ceiling(pagedChecks.TotalCount / 10.0),
            BaseUrl = "/Monitors/Detail",
            RouteValues = new Dictionary<string, string> { { "id", id } }
        };

        var incidents = (await _incidentRepo.GetByMonitorIdAsync(id)).ToList();
        var sslCheck = await _sslRepo.GetLatestByMonitorIdAsync(id);
        var group = string.IsNullOrEmpty(monitor.ContactGroupId) ? null : await _groupRepo.GetByIdAsync(monitor.ContactGroupId);

        Detail = new MonitorDetailDto
        {
            Id = monitor.Id,
            Name = monitor.Name,
            Url = monitor.Url,
            CurrentStatus = monitor.CurrentStatus,
            IsActive = monitor.IsActive,
            CheckIntervalSeconds = monitor.CheckIntervalSeconds,
            TimeoutSeconds = monitor.TimeoutSeconds,
            MaxResponseTimeMs = monitor.MaxResponseTimeMs,
            TriggerRate = monitor.TriggerRate,
            CheckSsl = monitor.CheckSsl,
            ContactGroupName = group?.Name,
            UptimePercent24h = uptime24h,
            UptimePercent7d = uptime7d,
            UptimePercent30d = uptime30d,
            AvgResponseTimeMs = avgResp,
            RecentChecks = pagedChecks.Items.Select(c => new CheckHistoryDto
            {
                Id = c.Id,
                CheckedAt = c.CheckedAt,
                IsSuccess = c.IsSuccess,
                StatusCode = c.StatusCode,
                ResponseTimeMs = c.ResponseTimeMs,
                ErrorMessage = c.ErrorMessage,
                CheckResultType = c.CheckResultType,
                HealthJson = c.HealthJson
            }).ToList(),
            RecentIncidents = incidents.Take(20).Select(i => new IncidentHistoryDto
            {
                Id = i.Id,
                StartedAt = i.StartedAt,
                ResolvedAt = i.ResolvedAt,
                Status = i.Status,
                Reason = i.Reason
            }).ToList(),
            SslInfo = sslCheck == null ? null : new SslInfoDto
            {
                ExpiryDate = sslCheck.ExpiryDate,
                DaysRemaining = sslCheck.DaysRemaining,
                IsValid = sslCheck.IsValid,
                CommonName = sslCheck.CommonName,
                Issuer = sslCheck.Issuer,
                ErrorMessage = sslCheck.ErrorMessage
            }
        };

        return Page();
    }

    public async Task<IActionResult> OnPostRunCheckAsync(string id)
    {
        var monitor = await _monitorRepo.GetByIdAsync(id);
        if (monitor == null) return NotFound();

        var httpClient = _httpClientFactory.CreateClient("MonitorClient");
        httpClient.Timeout = TimeSpan.FromSeconds(monitor.TimeoutSeconds);

        var check = await _checkService.CheckHttpAsync(monitor, httpClient, default);
        await _realtimeGraphService.BroadcastCheckResultAsync(check.IsSuccess);

        var openIncidentBefore = await _incidentRepo.GetOpenByMonitorIdAsync(monitor.Id);
        var incident = await _incidentService.ProcessCheckResultAsync(check, monitor);

        if (incident != null && incident.Status == Karavul.Core.Enums.IncidentStatus.Open)
        {
            await _notificationService.ProcessIncidentNotificationsAsync(incident, monitor, default);
        }
        else if (openIncidentBefore != null && incident == null)
        {
            var resolvedIncident = await _incidentRepo.GetByIdAsync(openIncidentBefore.Id);
            if (resolvedIncident != null)
                await _notificationService.ProcessRecoveryNotificationsAsync(resolvedIncident, monitor, default);
        }

        if (monitor.CheckSsl && monitor.Url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
        {
            await _checkService.CheckSslAsync(monitor, default);
        }

        return RedirectToPage(new { id = id });
    }
}
