using Karavul.Core.Enums;

namespace Karavul.Core.Entities;

/// <summary>
/// MonitorTarget olarak adlandırıldı; System.Threading.Monitor ile isim çakışmasını önlemek için.
/// </summary>
public class MonitorTarget
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public MonitorType MonitorType { get; set; } = MonitorType.Http;
    public string HttpMethod { get; set; } = "GET";
    public int ExpectedStatusCode { get; set; } = 200;
    public int CheckIntervalSeconds { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxResponseTimeMs { get; set; } = 5000;
    public bool IsActive { get; set; } = true;
    public bool CheckSsl { get; set; } = false;
    public int SslWarningDays { get; set; } = 30;
    public bool IsHealthJson { get; set; } = false;
    public string? ContactGroupId { get; set; }
    public string? Description { get; set; }
    public MonitorStatus CurrentStatus { get; set; } = MonitorStatus.Unknown;
    public DateTime? LastCheckedAt { get; set; }
    public int? LastStatusCode { get; set; }
    public long? LastResponseTimeMs { get; set; }
    public string? LastErrorMessage { get; set; }
    public int TriggerRate { get; set; } = 60;
    public bool IsInTriggerProcess { get; set; } = false;
    public DateTime? TriggerProcessStartedAt { get; set; }
    public int TriggerProcessFailCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
