using Dapper;
using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;

namespace Karavul.Data.Repositories;

public class NotificationLogRepository : INotificationLogRepository
{
    private readonly DbConnectionFactory _factory;

    public NotificationLogRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> CreateAsync(NotificationLog log)
    {
        if (string.IsNullOrEmpty(log.Id))
            log.Id = Guid.NewGuid().ToString();

        const string sql = """
            INSERT INTO NotificationLogs (Id, IncidentId, MonitorId, ContactGroupId, NotificationType,
                Recipient, Subject, Message, IsSuccess, ErrorMessage, SentAt)
            VALUES (@Id, @IncidentId, @MonitorId, @ContactGroupId, @NotificationType,
                @Recipient, @Subject, @Message, @IsSuccess, @ErrorMessage, @SentAt)
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            log.Id,
            log.IncidentId,
            log.MonitorId,
            log.ContactGroupId,
            NotificationType = (int)log.NotificationType,
            log.Recipient,
            log.Subject,
            log.Message,
            IsSuccess = log.IsSuccess ? 1 : 0,
            log.ErrorMessage,
            SentAt = log.SentAt.ToString("o")
        });
        return log.Id;
    }

    public async Task<(IEnumerable<NotificationLog> Items, int TotalCount)> GetPagedAsync(int page = 1, int pageSize = 10)
    {
        using var conn = _factory.CreateConnection();
        var totalCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM NotificationLogs");
        
        var results = await conn.QueryAsync<dynamic>(
            "SELECT * FROM NotificationLogs ORDER BY SentAt DESC LIMIT @Limit OFFSET @Offset",
            new { Limit = pageSize, Offset = (page - 1) * pageSize });
            
        return (results.Select(MapLog), totalCount);
    }

    public async Task<IEnumerable<NotificationLog>> GetByIncidentIdAsync(string incidentId)
    {
        using var conn = _factory.CreateConnection();
        var results = await conn.QueryAsync<dynamic>(
            "SELECT * FROM NotificationLogs WHERE IncidentId = @IncidentId ORDER BY SentAt DESC",
            new { IncidentId = incidentId });
        return results.Select(MapLog);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM NotificationLogs WHERE SentAt < @Cutoff",
            new { Cutoff = cutoff.ToString("o") });
    }

    private static NotificationLog MapLog(dynamic d) => new()
    {
        Id = d.Id,
        IncidentId = d.IncidentId,
        MonitorId = d.MonitorId,
        ContactGroupId = d.ContactGroupId,
        NotificationType = (Core.Enums.NotificationType)(d.NotificationType != null ? Convert.ToInt32(d.NotificationType) : 0),
        Recipient = d.Recipient ?? string.Empty,
        Subject = d.Subject ?? string.Empty,
        Message = d.Message ?? string.Empty,
        IsSuccess = Convert.ToInt32(d.IsSuccess) == 1,
        ErrorMessage = d.ErrorMessage,
        SentAt = DateTime.Parse((string)d.SentAt).ToUniversalTime()
    };
}
