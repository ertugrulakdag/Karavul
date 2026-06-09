using Karavul.Core.Enums;

namespace Karavul.Core.Entities;

public class Incident
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MonitorId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public string Reason { get; set; } = string.Empty;
    public string? LastErrorMessage { get; set; }
    public DateTime? LastNotificationAt { get; set; }
    public int NotificationCount { get; set; } = 0;
    public bool IsManuallyResolved { get; set; } = false;
    public string? ResolvedBy { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
