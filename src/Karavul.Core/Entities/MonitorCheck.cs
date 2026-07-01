using Karavul.Core.Enums;

namespace Karavul.Core.Entities;

public class MonitorCheck
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MonitorId { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
    public int? StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public CheckResultType CheckResultType { get; set; }
    public string? HealthJson { get; set; }
    public List<MonitorCheckHeader> Headers { get; set; } = new();
}
