using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.ContactGroups;

public class EditModel : PageModel
{
    private readonly IContactGroupRepository _repo;
    public EditModel(IContactGroupRepository repo) => _repo = repo;

    [BindProperty] public string Id { get; set; } = string.Empty;
    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public int RepeatAlertMinutes { get; set; }
    [BindProperty] public bool IsActive { get; set; }
    [BindProperty] public List<string> Emails { get; set; } = [];
    [BindProperty] public List<string> Phones { get; set; } = [];
    [BindProperty] public List<string> Telegrams { get; set; } = [];
    [BindProperty] public List<Karavul.Core.Enums.NotificationType> SelectedNotificationTypes { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var group = await _repo.GetByIdAsync(id);
        if (group == null) return NotFound();

        Id = group.Id;
        Name = group.Name;
        RepeatAlertMinutes = group.RepeatAlertMinutes;
        IsActive = group.IsActive;
        Emails = group.Emails.Select(e => e.Email).ToList();
        Phones = group.Phones.Select(p => p.PhoneNumber).ToList();
        Telegrams = group.Telegrams.Select(t => t.ChatId).ToList();
        UpdatedAt = group.UpdatedAt;
        UpdatedBy = group.UpdatedBy;

        SelectedNotificationTypes = [];
        if (group.ActiveNotificationTypes.HasFlag(Karavul.Core.Enums.NotificationType.Email)) SelectedNotificationTypes.Add(Karavul.Core.Enums.NotificationType.Email);
        if (group.ActiveNotificationTypes.HasFlag(Karavul.Core.Enums.NotificationType.Sms)) SelectedNotificationTypes.Add(Karavul.Core.Enums.NotificationType.Sms);
        if (group.ActiveNotificationTypes.HasFlag(Karavul.Core.Enums.NotificationType.Telegram)) SelectedNotificationTypes.Add(Karavul.Core.Enums.NotificationType.Telegram);

        return Page();
    }

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
            Id = Id,
            Name = Name.Trim(),
            RepeatAlertMinutes = RepeatAlertMinutes,
            IsActive = IsActive,
            ActiveNotificationTypes = activeTypes,
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

        await _repo.UpdateAsync(group);
        TempData["Success"] = $"'{Name}' grubu güncellendi.";
        return RedirectToPage("./Index");
    }
}
