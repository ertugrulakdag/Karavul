namespace Karavul.Core.Entities;

public class SslCertificateCheck
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MonitorId { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public int? DaysRemaining { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CommonName { get; set; }
    public string? Issuer { get; set; }
}
