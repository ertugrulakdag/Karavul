using System;
using Karavul.Core.Enums;

namespace Karavul.Core.DTOs;

public class IncidentDto
{
    public string Id { get; set; } = string.Empty;
    public string MonitorId { get; set; } = string.Empty;
    public string MonitorName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public IncidentStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? LastErrorMessage { get; set; }
    public int NotificationCount { get; set; }
    public bool IsManuallyResolved { get; set; }
    public string? ResolvedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
