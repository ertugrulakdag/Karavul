using Karavul.Core.Enums;

namespace Karavul.Core.Entities;

public class ContactGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public int RepeatAlertMinutes { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public NotificationType ActiveNotificationTypes { get; set; } = NotificationType.Email | NotificationType.Sms | NotificationType.Telegram;

    public List<ContactGroupMember> Members { get; set; } = [];
}
