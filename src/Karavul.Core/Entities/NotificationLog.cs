using Karavul.Core.Enums;

namespace Karavul.Core.Entities;

public class NotificationLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string IncidentId { get; set; } = string.Empty;
    public string MonitorId { get; set; } = string.Empty;
    public string? ContactGroupId { get; set; }
    public NotificationType NotificationType { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
