using Karavul.Core.Entities;

namespace Karavul.Core.Interfaces;

public interface IMonitorCheckRepository
{
    Task<string> CreateAsync(MonitorCheck check);
    Task<IEnumerable<MonitorCheck>> GetByMonitorIdAsync(string monitorId, int limit = 100);
    Task<(IEnumerable<MonitorCheck> Items, int TotalCount)> GetPagedByMonitorIdAsync(string monitorId, int page = 1, int pageSize = 10);
    Task<IEnumerable<MonitorCheck>> GetRecentAsync(string monitorId, DateTime since);
    Task DeleteOlderThanAsync(DateTime cutoff);
    Task<double> GetUptimePercentageAsync(string monitorId, DateTime since);
    Task<double> GetAverageResponseTimeAsync(string monitorId, DateTime since);
    Task<IEnumerable<dynamic>> GetResponseTimeHistoryAsync(DateTime since);
    Task<IEnumerable<dynamic>> GetStatusHistoryAsync(DateTime since, string groupByFormat);
}
