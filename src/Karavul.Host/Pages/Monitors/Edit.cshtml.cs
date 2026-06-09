using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Karavul.Host.Pages.Monitors;

public class EditModel : PageModel
{
    private readonly IMonitorRepository _repo;
    private readonly IContactGroupRepository _groupRepo;

    public EditModel(IMonitorRepository repo, IContactGroupRepository groupRepo)
    {
        _repo = repo;
        _groupRepo = groupRepo;
    }

    [BindProperty]
    public MonitorTarget Monitor { get; set; } = new();

    public List<SelectListItem> ContactGroups { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var monitor = await _repo.GetByIdAsync(id);
        if (monitor == null) return NotFound();

        Monitor = monitor;
        await LoadGroupsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadGroupsAsync();
            return Page();
        }

        var username = HttpContext.Session.GetString("Username") ?? "System";
        Monitor.UpdatedBy = username;
        await _repo.UpdateAsync(Monitor);
        TempData["Success"] = $"'{Monitor.Name}' güncellendi.";
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
