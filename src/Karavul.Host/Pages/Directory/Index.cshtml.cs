using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Directory;

public class IndexModel : PageModel
{
    private readonly IDirectoryContactRepository _repo;

    public IndexModel(IDirectoryContactRepository repo)
    {
        _repo = repo;
    }

    public List<DirectoryContact> Contacts { get; set; } = [];

    public async Task OnGetAsync()
    {
        var contacts = await _repo.GetAllAsync();
        Contacts = contacts.ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var userRole = HttpContext.Session.GetInt32("UserRole") ?? 8;
        var role = (Karavul.Core.Enums.UserRole)userRole;
        if (!role.HasFlag(Karavul.Core.Enums.UserRole.Admin) && !role.HasFlag(Karavul.Core.Enums.UserRole.Editor))
        {
            return Forbid();
        }

        await _repo.DeleteAsync(id);
        TempData["Success"] = "Kişi başarıyla silindi.";
        return RedirectToPage();
    }
}
