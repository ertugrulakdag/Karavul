using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.ContactGroups;

public class IndexModel : PageModel
{
    private readonly IContactGroupRepository _repo;
    private readonly IMonitorRepository _monitorRepo;
    public List<ContactGroup> Groups { get; set; } = [];

    public IndexModel(IContactGroupRepository repo, IMonitorRepository monitorRepo)
    {
        _repo = repo;
        _monitorRepo = monitorRepo;
    }

    public async Task OnGetAsync()
    {
        Groups = (await _repo.GetAllAsync()).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var monitors = await _monitorRepo.GetAllAsync();
        var linkedMonitors = monitors.Where(m => m.ContactGroupId == id).ToList();

        if (linkedMonitors.Any())
        {
            var names = string.Join(", ", linkedMonitors.Select(m => m.Name));
            TempData["Error"] = $"Bu iletişim grubu '{names}' adlı monitör(ler)e bağlı olduğu için silinemez.";
            return RedirectToPage();
        }

        await _repo.DeleteAsync(id);
        TempData["Success"] = "Contact group silindi.";
        return RedirectToPage();
    }
}
