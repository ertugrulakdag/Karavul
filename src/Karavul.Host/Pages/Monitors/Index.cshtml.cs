using Karavul.Core.Entities;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Monitors;

public class IndexModel : PageModel
{
    private readonly IMonitorRepository _repo;
    private readonly IIncidentRepository _incidentRepo;
    private readonly IContactGroupRepository _groupRepo;

    public IndexModel(IMonitorRepository repo, IIncidentRepository incidentRepo, IContactGroupRepository groupRepo)
    {
        _repo = repo;
        _incidentRepo = incidentRepo;
        _groupRepo = groupRepo;
    }

    public List<MonitorTarget> Monitors { get; set; } = [];
    public Dictionary<string, string> GroupNames { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public Karavul.Core.Enums.MonitorStatus? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterUrl { get; set; }

    public async Task OnGetAsync()
    {
        Monitors = (await _repo.GetAllAsync(FilterStatus, FilterName, FilterUrl)).ToList();
        var groups = await _groupRepo.GetAllAsync();
        GroupNames = groups.ToDictionary(g => g.Id, g => g.Name);
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        await _repo.DeleteAsync(id);
        TempData["Success"] = "Monitor silindi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(string id)
    {
        var monitor = await _repo.GetByIdAsync(id);
        if (monitor == null) return NotFound();

        monitor.IsActive = !monitor.IsActive;
        if (!monitor.IsActive)
            monitor.CurrentStatus = MonitorStatus.Paused;

        await _repo.UpdateAsync(monitor);
        TempData["Success"] = $"Monitor {(monitor.IsActive ? "aktif edildi" : "pasif edildi")}.";
        return RedirectToPage();
    }
}
