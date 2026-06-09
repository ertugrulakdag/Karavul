using Dapper;
using Karavul.Core.Entities;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;

namespace Karavul.Data.Repositories;

public class IncidentRepository : IIncidentRepository
{
    private readonly DbConnectionFactory _factory;

    public IncidentRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> CreateAsync(Incident incident)
    {
        if (string.IsNullOrEmpty(incident.Id))
            incident.Id = Guid.NewGuid().ToString();

        incident.CreatedAt = DateTime.UtcNow;

        using var conn = _factory.CreateConnection();

        while (true)
        {
            var tempGuid = Guid.NewGuid();
            var encoded = Karavul.Core.Helpers.GuidEncoder.Encode(tempGuid).Replace("-", "").Replace("_", "");
            incident.Code = encoded.Substring(0, Math.Min(8, encoded.Length));
            
            var exists = await conn.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM Incidents WHERE Code = @Code", new { Code = incident.Code });
            if (exists == 0) break;
        }

        const string sql = """
            INSERT INTO Incidents (Id, MonitorId, StartedAt, ResolvedAt, Status, Reason, 
                LastErrorMessage, LastNotificationAt, NotificationCount, CreatedAt, IsManuallyResolved, ResolvedBy, Code)
            VALUES (@Id, @MonitorId, @StartedAt, @ResolvedAt, @Status, @Reason,
                @LastErrorMessage, @LastNotificationAt, @NotificationCount, @CreatedAt, @IsManuallyResolved, @ResolvedBy, @Code)
            """;

        await conn.ExecuteAsync(sql, new
        {
            incident.Id,
            incident.MonitorId,
            StartedAt = incident.StartedAt.ToString("o"),
            ResolvedAt = incident.ResolvedAt?.ToString("o"),
            Status = (int)incident.Status,
            incident.Reason,
            incident.LastErrorMessage,
            LastNotificationAt = incident.LastNotificationAt?.ToString("o"),
            incident.NotificationCount,
            CreatedAt = incident.CreatedAt.ToString("o"),
            IsManuallyResolved = incident.IsManuallyResolved ? 1 : 0,
            incident.ResolvedBy,
            incident.Code
        });
        return incident.Id;
    }

    public async Task<Incident?> GetOpenByMonitorIdAsync(string monitorId)
    {
        using var conn = _factory.CreateConnection();
        var d = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM Incidents WHERE MonitorId = @MonitorId AND Status = 0 ORDER BY StartedAt DESC LIMIT 1",
            new { MonitorId = monitorId });

        return d == null ? null : MapIncident(d);
    }

    public async Task<(IEnumerable<Incident> Items, int TotalCount)> GetPagedAsync(int page = 1, int pageSize = 10, IncidentStatus? status = null, string? monitorName = null, string? code = null)
    {
        using var conn = _factory.CreateConnection();
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();
        
        parameters.Add("Limit", pageSize);
        parameters.Add("Offset", (page - 1) * pageSize);

        if (status.HasValue)
        {
            whereClauses.Add("i.Status = @Status");
            parameters.Add("Status", (int)status.Value);
        }

        if (!string.IsNullOrWhiteSpace(monitorName))
        {
            whereClauses.Add("m.Name LIKE @MonitorName");
            parameters.Add("MonitorName", $"%{monitorName}%");
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            whereClauses.Add("i.Code LIKE @Code");
            parameters.Add("Code", $"%{code.TrimStart('#')}%");
        }

        string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
        
        string countSql = $@"
            SELECT COUNT(*) 
            FROM Incidents i
            LEFT JOIN Monitors m ON i.MonitorId = m.Id
            {whereSql}";
            
        string dataSql = $@"
            SELECT i.* 
            FROM Incidents i
            LEFT JOIN Monitors m ON i.MonitorId = m.Id
            {whereSql}
            ORDER BY i.StartedAt DESC 
            LIMIT @Limit OFFSET @Offset";

        var totalCount = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var results = await conn.QueryAsync<dynamic>(dataSql, parameters);

        return (results.Select(MapIncident), totalCount);
    }

    public async Task<IEnumerable<Incident>> GetByMonitorIdAsync(string monitorId)
    {
        using var conn = _factory.CreateConnection();
        var results = await conn.QueryAsync<dynamic>(
            "SELECT * FROM Incidents WHERE MonitorId = @MonitorId ORDER BY StartedAt DESC",
            new { MonitorId = monitorId });
        return results.Select(MapIncident);
    }

    public async Task<Incident?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        var d = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM Incidents WHERE Id = @Id", new { Id = id });
        return d == null ? null : MapIncident(d);
    }

    public async Task UpdateAsync(Incident incident)
    {
        const string sql = """
            UPDATE Incidents SET
                LastErrorMessage = @LastErrorMessage,
                LastNotificationAt = @LastNotificationAt,
                NotificationCount = @NotificationCount,
                Status = @Status,
                ResolvedAt = @ResolvedAt,
                IsManuallyResolved = @IsManuallyResolved,
                ResolvedBy = @ResolvedBy
            WHERE Id = @Id
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            incident.Id,
            incident.LastErrorMessage,
            LastNotificationAt = incident.LastNotificationAt?.ToString("o"),
            incident.NotificationCount,
            Status = (int)incident.Status,
            ResolvedAt = incident.ResolvedAt?.ToString("o"),
            IsManuallyResolved = incident.IsManuallyResolved ? 1 : 0,
            incident.ResolvedBy
        });
    }

    public async Task ResolveAsync(string id, DateTime resolvedAt, bool isManuallyResolved = false, string? resolvedBy = null)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE Incidents SET Status = 1, ResolvedAt = @ResolvedAt, IsManuallyResolved = @IsManuallyResolved, ResolvedBy = @ResolvedBy WHERE Id = @Id",
            new { Id = id, ResolvedAt = resolvedAt.ToString("o"), IsManuallyResolved = isManuallyResolved ? 1 : 0, ResolvedBy = resolvedBy });
    }

    public async Task<int> GetActiveCountAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM Incidents WHERE Status = 0");
    }

    private static Incident MapIncident(dynamic d) => new()
    {
        Id = d.Id,
        MonitorId = d.MonitorId,
        StartedAt = DateTime.Parse((string)d.StartedAt).ToUniversalTime(),
        ResolvedAt = d.ResolvedAt != null ? DateTime.Parse((string)d.ResolvedAt).ToUniversalTime() : null,
        Status = (IncidentStatus)(d.Status != null ? Convert.ToInt32(d.Status) : 0),
        Reason = d.Reason ?? string.Empty,
        LastErrorMessage = d.LastErrorMessage,
        LastNotificationAt = d.LastNotificationAt != null ? DateTime.Parse((string)d.LastNotificationAt).ToUniversalTime() : null,
        NotificationCount = d.NotificationCount != null ? Convert.ToInt32(d.NotificationCount) : 0,
        IsManuallyResolved = d.IsManuallyResolved != null && Convert.ToBoolean(d.IsManuallyResolved),
        ResolvedBy = d.ResolvedBy,
        Code = d.Code ?? string.Empty,
        CreatedAt = DateTime.Parse((string)d.CreatedAt).ToUniversalTime()
    };
}
