using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.NotificationLogs;

public class IndexModel : PageModel
{
    private readonly INotificationLogRepository _repo;
    private readonly IMonitorRepository _monitorRepo;

    public IndexModel(INotificationLogRepository repo, IMonitorRepository monitorRepo)
    {
        _repo = repo;
        _monitorRepo = monitorRepo;
    }

    public List<NotificationLog> Logs { get; set; } = [];
    public Dictionary<string, string> MonitorNames { get; set; } = [];
    public Karavul.Core.DTOs.PaginationModel Pagination { get; set; } = new();

    public async Task OnGetAsync(int p = 1)
    {
        var paged = await _repo.GetPagedAsync(p, 10);
        Logs = paged.Items.ToList();
        
        Pagination = new Karavul.Core.DTOs.PaginationModel
        {
            CurrentPage = p,
            TotalRecords = paged.TotalCount,
            TotalPages = (int)Math.Ceiling(paged.TotalCount / 10.0),
            BaseUrl = "/NotificationLogs"
        };
        
        var monitors = await _monitorRepo.GetAllAsync();
        MonitorNames = monitors.ToDictionary(m => m.Id, m => m.Name);
    }
}
