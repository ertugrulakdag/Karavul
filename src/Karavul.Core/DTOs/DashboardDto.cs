using Karavul.Core.Entities;
using Karavul.Core.Enums;

namespace Karavul.Core.DTOs;

public class DashboardDto
{
    public int TotalMonitors { get; set; }
    public int UpMonitors { get; set; }
    public int DownMonitors { get; set; }
    public int WarningMonitors { get; set; }
    public int PausedMonitors { get; set; }
    public int ActiveIncidents { get; set; }
    public double Last24hUptimePercent { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public List<MonitorSummaryDto> Monitors { get; set; } = [];
}

public class MonitorSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public MonitorStatus CurrentStatus { get; set; }
    public DateTime? LastCheckedAt { get; set; }
    public int? LastStatusCode { get; set; }
    public long? LastResponseTimeMs { get; set; }
    public string? LastErrorMessage { get; set; }
    public string? ContactGroupName { get; set; }
    public bool IsActive { get; set; }
    public double UptimePercent24h { get; set; }
}
