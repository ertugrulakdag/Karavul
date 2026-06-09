using Karavul.Core.Entities;
using Karavul.Core.Enums;

namespace Karavul.Core.Interfaces;

public interface IIncidentRepository
{
    Task<string> CreateAsync(Incident incident);
    Task<Incident?> GetOpenByMonitorIdAsync(string monitorId);
    Task<(IEnumerable<Incident> Items, int TotalCount)> GetPagedAsync(int page = 1, int pageSize = 10, IncidentStatus? status = null, string? monitorName = null, string? code = null);
    Task<IEnumerable<Incident>> GetByMonitorIdAsync(string monitorId);
    Task<Incident?> GetByIdAsync(string id);
    Task UpdateAsync(Incident incident);
    Task ResolveAsync(string id, DateTime resolvedAt, bool isManuallyResolved = false, string? resolvedBy = null);
    Task<int> GetActiveCountAsync();
}
