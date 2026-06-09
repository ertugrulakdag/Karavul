using Karavul.Core.Enums;

namespace Karavul.Core.DTOs;

public class MonitorDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public MonitorStatus CurrentStatus { get; set; }
    public bool IsActive { get; set; }
    public int CheckIntervalSeconds { get; set; }
    public int TimeoutSeconds { get; set; }
    public int MaxResponseTimeMs { get; set; }
    public bool CheckSsl { get; set; }
    public string? ContactGroupName { get; set; }
    public double UptimePercent24h { get; set; }
    public double UptimePercent7d { get; set; }
    public double UptimePercent30d { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public List<CheckHistoryDto> RecentChecks { get; set; } = [];
    public List<IncidentHistoryDto> RecentIncidents { get; set; } = [];
    public SslInfoDto? SslInfo { get; set; }
}

public class CheckHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public bool IsSuccess { get; set; }
    public int? StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public CheckResultType CheckResultType { get; set; }
    public string? HealthJson { get; set; }
}

public class IncidentHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public IncidentStatus Status { get; set; }
    public string Reason { get; set; } = string.Empty;
    public TimeSpan? Duration => ResolvedAt.HasValue ? ResolvedAt.Value - StartedAt : DateTime.UtcNow - StartedAt;
}

public class SslInfoDto
{
    public DateTime? ExpiryDate { get; set; }
    public int? DaysRemaining { get; set; }
    public bool IsValid { get; set; }
    public string? CommonName { get; set; }
    public string? Issuer { get; set; }
    public string? ErrorMessage { get; set; }
}
