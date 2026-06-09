using Karavul.Core.Entities;

namespace Karavul.Core.Interfaces;

public interface ISslCheckRepository
{
    Task<string> CreateAsync(SslCertificateCheck check);
    Task<SslCertificateCheck?> GetLatestByMonitorIdAsync(string monitorId);
    Task<IEnumerable<SslCertificateCheck>> GetByMonitorIdAsync(string monitorId, int limit = 50);
    Task DeleteOlderThanAsync(DateTime cutoff);
}
