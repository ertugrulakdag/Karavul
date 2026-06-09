using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Incidents;

public class IndexModel : PageModel
{
    private readonly IIncidentRepository _incidentRepo;
    private readonly IMonitorRepository _monitorRepo;
    private readonly Karavul.Services.NotificationService _notificationService;

    public IndexModel(
        IIncidentRepository incidentRepo, 
        IMonitorRepository monitorRepo,
        Karavul.Services.NotificationService notificationService)
    {
        _incidentRepo = incidentRepo;
        _monitorRepo = monitorRepo;
        _notificationService = notificationService;
    }

    public List<Karavul.Core.DTOs.IncidentDto> Incidents { get; set; } = [];
    public Dictionary<string, string> MonitorNames { get; set; } = [];
    public Karavul.Core.DTOs.PaginationModel Pagination { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public Karavul.Core.Enums.IncidentStatus? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterMonitor { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterCode { get; set; }

    public async Task OnGetAsync(int p = 1)
    {
        var paged = await _incidentRepo.GetPagedAsync(p, 10, FilterStatus, FilterMonitor, FilterCode);

        Pagination = new Karavul.Core.DTOs.PaginationModel
        {
            CurrentPage = p,
            TotalRecords = paged.TotalCount,
            TotalPages = (int)Math.Ceiling(paged.TotalCount / 10.0),
            BaseUrl = "/Incidents"
        };
        
        if (FilterStatus.HasValue)
            Pagination.RouteValues["FilterStatus"] = ((int)FilterStatus.Value).ToString();
        if (!string.IsNullOrEmpty(FilterMonitor))
            Pagination.RouteValues["FilterMonitor"] = FilterMonitor;
        if (!string.IsNullOrEmpty(FilterCode))
            Pagination.RouteValues["FilterCode"] = FilterCode;
            
        var monitors = await _monitorRepo.GetAllAsync();
        MonitorNames = monitors.ToDictionary(m => m.Id, m => m.Name);

        Incidents = paged.Items.Select(i => new Karavul.Core.DTOs.IncidentDto
        {
            Id = i.Id,
            MonitorId = i.MonitorId,
            MonitorName = MonitorNames.TryGetValue(i.MonitorId, out var mn) ? mn : i.MonitorId,
            StartedAt = i.StartedAt,
            ResolvedAt = i.ResolvedAt,
            Status = i.Status,
            Reason = i.Reason,
            LastErrorMessage = i.LastErrorMessage,
            NotificationCount = i.NotificationCount,
            IsManuallyResolved = i.IsManuallyResolved,
            ResolvedBy = i.ResolvedBy,
            Code = i.Code,
            CreatedAt = i.CreatedAt
        }).ToList();
    }

    public async Task<IActionResult> OnPostResolveAsync(string id)
    {
        var username = HttpContext.Session.GetString("Username") ?? "System";
        await _incidentRepo.ResolveAsync(id, DateTime.UtcNow, isManuallyResolved: true, resolvedBy: username);
        
        var incident = await _incidentRepo.GetByIdAsync(id);
        if (incident != null)
        {
            var monitor = await _monitorRepo.GetByIdAsync(incident.MonitorId);
            if (monitor != null)
            {
                await _notificationService.ProcessRecoveryNotificationsAsync(incident, monitor, default);
                await _monitorRepo.UpdateStatusAsync(monitor.Id, Karavul.Core.Enums.MonitorStatus.Up, monitor.LastStatusCode, monitor.LastResponseTimeMs, null);
            }
        }
        
        return RedirectToPage();
    }
}
