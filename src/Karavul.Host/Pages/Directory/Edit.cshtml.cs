using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Directory;

public class EditModel : PageModel
{
    private readonly IDirectoryContactRepository _repo;

    public EditModel(IDirectoryContactRepository repo)
    {
        _repo = repo;
    }

    [BindProperty]
    public DirectoryContact Contact { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var contact = await _repo.GetByIdAsync(id);
        if (contact == null)
        {
            TempData["Error"] = "Kişi bulunamadı.";
            return RedirectToPage("Index");
        }

        Contact = contact;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var existing = await _repo.GetByIdAsync(Contact.Id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.FirstName = Contact.FirstName;
        existing.LastName = Contact.LastName;
        existing.Email = Contact.Email ?? string.Empty;
        existing.PhoneNumber = Contact.PhoneNumber ?? string.Empty;
        existing.TelegramChatId = Contact.TelegramChatId ?? string.Empty;
        existing.IsActive = Contact.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = HttpContext.Session.GetString("Username");

        await _repo.UpdateAsync(existing);

        TempData["Success"] = "Kişi başarıyla güncellendi.";
        return RedirectToPage("Index");
    }
}
