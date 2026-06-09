using Dapper;
using Karavul.Core.Entities;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;

namespace Karavul.Data.Repositories;

public class MonitorRepository : IMonitorRepository
{
    private readonly DbConnectionFactory _factory;

    public MonitorRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<MonitorTarget>> GetAllAsync(MonitorStatus? status = null, string? name = null, string? url = null)
    {
        using var conn = _factory.CreateConnection();
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (status.HasValue)
        {
            if (status.Value == MonitorStatus.Paused)
            {
                whereClauses.Add("(IsActive = 0 OR CurrentStatus = @Status)");
                parameters.Add("Status", (int)MonitorStatus.Paused);
            }
            else
            {
                whereClauses.Add("CurrentStatus = @Status AND IsActive = 1");
                parameters.Add("Status", (int)status.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("Name LIKE @Name");
            parameters.Add("Name", $"%{name}%");
        }

        if (!string.IsNullOrWhiteSpace(url))
        {
            whereClauses.Add("Url LIKE @Url");
            parameters.Add("Url", $"%{url}%");
        }
        
        string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
        string sql = $"SELECT * FROM Monitors {whereSql} ORDER BY Name";
        
        return await conn.QueryAsync<MonitorTarget>(sql, parameters);
    }

    public async Task<IEnumerable<MonitorTarget>> GetActiveAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<MonitorTarget>(
            "SELECT * FROM Monitors WHERE IsActive = 1 ORDER BY Name");
    }

    public async Task<MonitorTarget?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<MonitorTarget>(
            "SELECT * FROM Monitors WHERE Id = @Id", new { Id = id });
    }

    public async Task<string> CreateAsync(MonitorTarget monitor)
    {
        if (string.IsNullOrEmpty(monitor.Id))
            monitor.Id = Guid.NewGuid().ToString();

        monitor.CreatedAt = DateTime.UtcNow;
        monitor.UpdatedAt = DateTime.UtcNow;

        const string sql = """
            INSERT INTO Monitors (Id, Name, Url, MonitorType, HttpMethod, ExpectedStatusCode,
                CheckIntervalSeconds, TimeoutSeconds, MaxResponseTimeMs, IsActive, CheckSsl,
                SslWarningDays, IsHealthJson, ContactGroupId, Description, CurrentStatus, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
            VALUES (@Id, @Name, @Url, @MonitorType, @HttpMethod, @ExpectedStatusCode,
                @CheckIntervalSeconds, @TimeoutSeconds, @MaxResponseTimeMs, @IsActive, @CheckSsl,
                @SslWarningDays, @IsHealthJson, @ContactGroupId, @Description, @CurrentStatus, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy)
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, monitor);
        return monitor.Id;
    }

    public async Task UpdateAsync(MonitorTarget monitor)
    {
        monitor.UpdatedAt = DateTime.UtcNow;

        const string sql = """
            UPDATE Monitors SET
                Name = @Name,
                Url = @Url,
                MonitorType = @MonitorType,
                HttpMethod = @HttpMethod,
                ExpectedStatusCode = @ExpectedStatusCode,
                CheckIntervalSeconds = @CheckIntervalSeconds,
                TimeoutSeconds = @TimeoutSeconds,
                MaxResponseTimeMs = @MaxResponseTimeMs,
                IsActive = @IsActive,
                CheckSsl = @CheckSsl,
                SslWarningDays = @SslWarningDays,
                IsHealthJson = @IsHealthJson,
                ContactGroupId = @ContactGroupId,
                Description = @Description,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, monitor);
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Monitors WHERE Id = @Id", new { Id = id });
    }

    public async Task UpdateStatusAsync(string id, MonitorStatus status, int? statusCode, long? responseTimeMs, string? errorMessage)
    {
        const string sql = """
            UPDATE Monitors SET
                CurrentStatus = @Status,
                LastCheckedAt = @Now,
                LastStatusCode = @StatusCode,
                LastResponseTimeMs = @ResponseTimeMs,
                LastErrorMessage = @ErrorMessage,
                UpdatedAt = @Now
            WHERE Id = @Id
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            Id = id,
            Status = (int)status,
            Now = DateTime.UtcNow.ToString("o"),
            StatusCode = statusCode,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage
        });
    }
}
