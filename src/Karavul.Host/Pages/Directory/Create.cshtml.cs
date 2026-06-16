using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Directory;

public class CreateModel : PageModel
{
    private readonly IDirectoryContactRepository _repo;

    public CreateModel(IDirectoryContactRepository repo)
    {
        _repo = repo;
    }

    [BindProperty]
    public DirectoryContact Contact { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        Contact.CreatedBy = HttpContext.Session.GetString("Username");
        Contact.UpdatedBy = Contact.CreatedBy;

        await _repo.CreateAsync(Contact);

        TempData["Success"] = "Kişi rehbere başarıyla eklendi.";
        return RedirectToPage("Index");
    }
}
