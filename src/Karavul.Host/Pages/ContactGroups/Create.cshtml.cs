using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.ContactGroups;

public class CreateModel : PageModel
{
    private readonly IContactGroupRepository _repo;
    private readonly IDirectoryContactRepository _directoryRepo;

    public CreateModel(IContactGroupRepository repo, IDirectoryContactRepository directoryRepo)
    {
        _repo = repo;
        _directoryRepo = directoryRepo;
    }

    [BindProperty] public string Name { get; set; } = string.Empty;
    [BindProperty] public int RepeatAlertMinutes { get; set; } = 0;
    [BindProperty] public bool IsActive { get; set; } = true;
    [BindProperty] public List<ContactGroupMember> Members { get; set; } = [];
    [BindProperty] public List<Karavul.Core.Enums.NotificationType> SelectedNotificationTypes { get; set; } = [Karavul.Core.Enums.NotificationType.Email, Karavul.Core.Enums.NotificationType.Sms, Karavul.Core.Enums.NotificationType.Telegram];

    public List<DirectoryContact> DirectoryContacts { get; set; } = [];

    public async Task OnGetAsync() 
    { 
        var contacts = await _directoryRepo.GetAllAsync();
        DirectoryContacts = contacts.Where(c => c.IsActive).ToList();
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
            Name = Name.Trim(),
            RepeatAlertMinutes = RepeatAlertMinutes,
            IsActive = IsActive,
            ActiveNotificationTypes = activeTypes,
            CreatedBy = username,
            UpdatedBy = username,
            Members = Members.Where(m => !string.IsNullOrWhiteSpace(m.FirstName) || !string.IsNullOrWhiteSpace(m.Email) || !string.IsNullOrWhiteSpace(m.PhoneNumber) || !string.IsNullOrWhiteSpace(m.TelegramChatId)).ToList()
        };

        await _repo.CreateAsync(group);
        TempData["Success"] = $"'{Name}' grubu oluşturuldu.";
        return RedirectToPage("./Index");
    }
}
