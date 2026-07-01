using Dapper;
using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;

namespace Karavul.Data.Repositories;

public class MonitorCheckRepository : IMonitorCheckRepository
{
    private readonly DbConnectionFactory _factory;

    public MonitorCheckRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> CreateAsync(MonitorCheck check)
    {
        if (string.IsNullOrEmpty(check.Id))
            check.Id = Guid.NewGuid().ToString();

        const string sql = """
            INSERT INTO MonitorChecks (Id, MonitorId, CheckedAt, IsSuccess, StatusCode, ResponseTimeMs, ErrorMessage, CheckResultType, HealthJson)
            VALUES (@Id, @MonitorId, @CheckedAt, @IsSuccess, @StatusCode, @ResponseTimeMs, @ErrorMessage, @CheckResultType, @HealthJson)
            """;

        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync(sql, new
            {
                check.Id,
                check.MonitorId,
                CheckedAt = check.CheckedAt.ToString("o"),
                IsSuccess = check.IsSuccess ? 1 : 0,
                check.StatusCode,
                check.ResponseTimeMs,
                check.ErrorMessage,
                CheckResultType = (int)check.CheckResultType,
                check.HealthJson
            }, transaction);

            if (check.Headers != null && check.Headers.Any())
            {
                const string headerSql = """
                    INSERT INTO MonitorCheckHeaders (Id, MonitorCheckId, Name, Value)
                    VALUES (@Id, @MonitorCheckId, @Name, @Value)
                    """;
                await conn.ExecuteAsync(headerSql, check.Headers.Select(h => new
                {
                    h.Id,
                    h.MonitorCheckId,
                    h.Name,
                    h.Value
                }), transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return check.Id;
    }

    public async Task<IEnumerable<MonitorCheck>> GetByMonitorIdAsync(string monitorId, int limit = 100)
    {
        using var conn = _factory.CreateConnection();
        var results = await conn.QueryAsync<dynamic>(
            "SELECT * FROM MonitorChecks WHERE MonitorId = @MonitorId ORDER BY CheckedAt DESC LIMIT @Limit",
            new { MonitorId = monitorId, Limit = limit });

        return results.Select(MapCheck);
    }

    public async Task<(IEnumerable<MonitorCheck> Items, int TotalCount)> GetPagedByMonitorIdAsync(string monitorId, int page = 1, int pageSize = 10)
    {
        using var conn = _factory.CreateConnection();
        var totalCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM MonitorChecks WHERE MonitorId = @MonitorId",
            new { MonitorId = monitorId });

        var results = await conn.QueryAsync<dynamic>(
            "SELECT * FROM MonitorChecks WHERE MonitorId = @MonitorId ORDER BY CheckedAt DESC LIMIT @Limit OFFSET @Offset",
            new { MonitorId = monitorId, Limit = pageSize, Offset = (page - 1) * pageSize });

        var checks = results.Select(MapCheck).ToList();
        
        if (checks.Any())
        {
            var checkIds = checks.Select(c => c.Id).ToArray();
            var headers = await conn.QueryAsync<MonitorCheckHeader>(
                "SELECT * FROM MonitorCheckHeaders WHERE MonitorCheckId IN @Ids",
                new { Ids = checkIds });
            var headersLookup = headers.ToLookup(h => h.MonitorCheckId);
            foreach (var c in checks)
            {
                c.Headers = headersLookup[c.Id].ToList();
            }
        }

        return (checks, totalCount);
    }

    public async Task<IEnumerable<MonitorCheck>> GetRecentAsync(string monitorId, DateTime since)
    {
        using var conn = _factory.CreateConnection();
        var results = await conn.QueryAsync<dynamic>(
            "SELECT * FROM MonitorChecks WHERE MonitorId = @MonitorId AND CheckedAt >= @Since ORDER BY CheckedAt DESC",
            new { MonitorId = monitorId, Since = since.ToString("o") });

        return results.Select(MapCheck);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM MonitorChecks WHERE CheckedAt < @Cutoff",
            new { Cutoff = cutoff.ToString("o") });
    }

    private class UptimeResult { public long Total { get; set; } public long Successful { get; set; } }

    public async Task<double> GetUptimePercentageAsync(string monitorId, DateTime since)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<UptimeResult>(
            """
            SELECT 
                COUNT(*) as Total,
                SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) as Successful
            FROM MonitorChecks 
            WHERE MonitorId = @MonitorId AND CheckedAt >= @Since
            """,
            new { MonitorId = monitorId, Since = since.ToString("o") });

        if (result == null || result.Total == 0) return 100.0;
        return Math.Round((double)result.Successful / result.Total * 100.0, 2);
    }

    public async Task<double> GetAverageResponseTimeAsync(string monitorId, DateTime since)
    {
        using var conn = _factory.CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<double?>(
            "SELECT AVG(ResponseTimeMs) FROM MonitorChecks WHERE MonitorId = @MonitorId AND CheckedAt >= @Since AND IsSuccess = 1",
            new { MonitorId = monitorId, Since = since.ToString("o") });

        return Math.Round(result ?? 0.0, 0);
    }

    public async Task<IEnumerable<dynamic>> GetResponseTimeHistoryAsync(DateTime since)
    {
        using var conn = _factory.CreateConnection();
        // SQLite 'strftime' groups by minute: '%Y-%m-%dT%H:%M:00Z'
        var sql = """
            SELECT 
                strftime('%H:%M', CheckedAt) as TimeLabel, 
                AVG(ResponseTimeMs) as AvgResponseTime
            FROM MonitorChecks 
            WHERE CheckedAt >= @Since AND IsSuccess = 1
            GROUP BY strftime('%Y-%m-%d %H:%M', CheckedAt)
            ORDER BY strftime('%Y-%m-%d %H:%M', CheckedAt) ASC
            """;
        
        var result = await conn.QueryAsync(sql, new { Since = since.ToString("o") });
        return result;
    }

    public async Task<IEnumerable<dynamic>> GetStatusHistoryAsync(DateTime since, string groupByFormat)
    {
        using var conn = _factory.CreateConnection();
        // SQLite strftime takes format and time string
        var sql = @"
            SELECT 
                strftime(@GroupByFormat, CheckedAt) as TimeGroup,
                SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) as SuccessCount,
                SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) as FailCount
            FROM MonitorChecks
            WHERE CheckedAt >= @Since
            GROUP BY TimeGroup
            ORDER BY TimeGroup ASC
        ";

        var result = await conn.QueryAsync(sql, new { Since = since.ToString("o"), GroupByFormat = groupByFormat });
        return result;
    }

    private static MonitorCheck MapCheck(dynamic d) => new()
    {
        Id = d.Id,
        MonitorId = d.MonitorId,
        CheckedAt = DateTime.Parse((string)d.CheckedAt).ToUniversalTime(),
        IsSuccess = Convert.ToInt32(d.IsSuccess) == 1,
        StatusCode = d.StatusCode != null ? Convert.ToInt32(d.StatusCode) : null,
        ResponseTimeMs = d.ResponseTimeMs != null ? Convert.ToInt64(d.ResponseTimeMs) : 0,
        ErrorMessage = d.ErrorMessage,
        CheckResultType = (Core.Enums.CheckResultType)(d.CheckResultType != null ? Convert.ToInt32(d.CheckResultType) : 0),
        HealthJson = d.HealthJson
    };
}
