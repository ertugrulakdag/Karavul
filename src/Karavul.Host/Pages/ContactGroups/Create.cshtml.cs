using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.ContactGroups;

public class CreateModel : PageModel
{
    private readonly IContactGroupRepository _repo;
    public CreateModel(IContactGroupRepository repo) => _repo = repo;

    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public int RepeatAlertMinutes { get; set; } = 0;
    [BindProperty] public bool IsActive { get; set; } = true;
    [BindProperty] public List<string> Emails { get; set; } = [];
    [BindProperty] public List<string> Phones { get; set; } = [];
    [BindProperty] public List<string> Telegrams { get; set; } = [];
    [BindProperty] public List<Karavul.Core.Enums.NotificationType> SelectedNotificationTypes { get; set; } = [Karavul.Core.Enums.NotificationType.Email, Karavul.Core.Enums.NotificationType.Sms, Karavul.Core.Enums.NotificationType.Telegram];

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError("", "Grup adı gereklidir.");
            return Page();
        }

        var activeTypes = Karavul.Core.Enums.NotificationType.None;
        if (SelectedNotificationTypes != null)
        {
            foreach (var t in SelectedNotificationTypes)
                activeTypes |= t;
        }

        var username = HttpContext.Session.GetString("Username") ?? "System";
        var group = new ContactGroup
        {
            Name = Name.Trim(),
            RepeatAlertMinutes = RepeatAlertMinutes,
            IsActive = IsActive,
            ActiveNotificationTypes = activeTypes,
            CreatedBy = username,
            UpdatedBy = username,
            Emails = Emails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => new ContactGroupEmail { Email = e.Trim() })
                .ToList(),
            Phones = Phones
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => new ContactGroupPhone { PhoneNumber = p.Trim() })
                .ToList(),
            Telegrams = Telegrams
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => new ContactGroupTelegram { ChatId = t.Trim() })
                .ToList()
        };

        await _repo.CreateAsync(group);
        TempData["Success"] = $"'{Name}' grubu oluşturuldu.";
        return RedirectToPage("./Index");
    }
}
