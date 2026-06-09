using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Karavul.Host.Pages.Monitors;

public class CreateModel : PageModel
{
    private readonly IMonitorRepository _repo;
    private readonly IContactGroupRepository _groupRepo;

    public CreateModel(IMonitorRepository repo, IContactGroupRepository groupRepo)
    {
        _repo = repo;
        _groupRepo = groupRepo;
    }

    [BindProperty]
    public MonitorTarget Monitor { get; set; } = new()
    {
        HttpMethod = "GET",
        ExpectedStatusCode = 200,
        CheckIntervalSeconds = 60,
        TimeoutSeconds = 30,
        MaxResponseTimeMs = 5000,
        IsActive = true,
        CheckSsl = false,
        SslWarningDays = 30
    };

    public List<SelectListItem> ContactGroups { get; set; } = [];

    public async Task OnGetAsync()
    {
        await LoadGroupsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadGroupsAsync();
            return Page();
        }

        Monitor.Id = Guid.NewGuid().ToString();
        var username = HttpContext.Session.GetString("Username") ?? "System";
        Monitor.CreatedBy = username;
        Monitor.UpdatedBy = username;
        await _repo.CreateAsync(Monitor);

        TempData["Success"] = $"'{Monitor.Name}' monitoru eklendi.";
        return RedirectToPage("./Index");
    }

    private async Task LoadGroupsAsync()
    {
        var groups = await _groupRepo.GetAllAsync();
        ContactGroups = groups
            .Where(g => g.IsActive)
            .Select(g => new SelectListItem { Value = g.Id, Text = g.Name })
            .ToList();
        ContactGroups.Insert(0, new SelectListItem { Value = "", Text = "- Seçin -" });
    }
}
