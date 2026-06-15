using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.ContactGroups;

public class EditModel : PageModel
{
    private readonly IContactGroupRepository _repo;
    private readonly IDirectoryContactRepository _directoryRepo;

    public EditModel(IContactGroupRepository repo, IDirectoryContactRepository directoryRepo)
    {
        _repo = repo;
        _directoryRepo = directoryRepo;
    }

    [BindProperty] public string Id { get; set; } = string.Empty;
    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public int RepeatAlertMinutes { get; set; }
    [BindProperty] public bool IsActive { get; set; }
    [BindProperty] public List<ContactGroupMember> Members { get; set; } = [];
    [BindProperty] public List<Karavul.Core.Enums.NotificationType> SelectedNotificationTypes { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public List<DirectoryContact> DirectoryContacts { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(string id)
    {
        var group = await _repo.GetByIdAsync(id);
        if (group == null) return NotFound();

        Id = group.Id;
        Name = group.Name;
        RepeatAlertMinutes = group.RepeatAlertMinutes;
        IsActive = group.IsActive;
        Members = group.Members.ToList();
        UpdatedAt = group.UpdatedAt;
        UpdatedBy = group.UpdatedBy;

        SelectedNotificationTypes = [];
        if (group.ActiveNotificationTypes.HasFlag(Karavul.Core.Enums.NotificationType.Email)) SelectedNotificationTypes.Add(Karavul.Core.Enums.NotificationType.Email);
        if (group.ActiveNotificationTypes.HasFlag(Karavul.Core.Enums.NotificationType.Sms)) SelectedNotificationTypes.Add(Karavul.Core.Enums.NotificationType.Sms);
        if (group.ActiveNotificationTypes.HasFlag(Karavul.Core.Enums.NotificationType.Telegram)) SelectedNotificationTypes.Add(Karavul.Core.Enums.NotificationType.Telegram);

        var contacts = await _directoryRepo.GetAllAsync();
        DirectoryContacts = contacts.Where(c => c.IsActive).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError("", "Grup adı gereklidir.");
            var contacts = await _directoryRepo.GetAllAsync();
            DirectoryContacts = contacts.Where(c => c.IsActive).ToList();
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
            Members = Members.Where(m => !string.IsNullOrWhiteSpace(m.FirstName) || !string.IsNullOrWhiteSpace(m.Email) || !string.IsNullOrWhiteSpace(m.PhoneNumber) || !string.IsNullOrWhiteSpace(m.TelegramChatId)).ToList()
        };

        await _repo.UpdateAsync(group);
        TempData["Success"] = $"'{Name}' grubu güncellendi.";
        return RedirectToPage("./Index");
    }
}
