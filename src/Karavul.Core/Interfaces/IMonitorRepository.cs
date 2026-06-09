using Karavul.Core.Entities;
using Karavul.Core.Enums;

namespace Karavul.Core.Interfaces;

public interface IMonitorRepository
{
    Task<IEnumerable<MonitorTarget>> GetAllAsync(MonitorStatus? status = null, string? name = null, string? url = null);
    Task<IEnumerable<MonitorTarget>> GetActiveAsync();
    Task<MonitorTarget?> GetByIdAsync(string id);
    Task<string> CreateAsync(MonitorTarget monitor);
    Task UpdateAsync(MonitorTarget monitor);
    Task DeleteAsync(string id);
    Task UpdateStatusAsync(string id, MonitorStatus status, int? statusCode, long? responseTimeMs, string? errorMessage);
}
