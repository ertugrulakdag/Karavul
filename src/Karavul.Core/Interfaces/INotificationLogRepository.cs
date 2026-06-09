using Karavul.Core.Entities;

namespace Karavul.Core.Interfaces;

public interface INotificationLogRepository
{
    Task<string> CreateAsync(NotificationLog log);
    Task<(IEnumerable<NotificationLog> Items, int TotalCount)> GetPagedAsync(int page = 1, int pageSize = 10);
    Task<IEnumerable<NotificationLog>> GetByIncidentIdAsync(string incidentId);
    Task DeleteOlderThanAsync(DateTime cutoff);
}
