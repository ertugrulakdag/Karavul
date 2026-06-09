using Dapper;
using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;

namespace Karavul.Data.Repositories;

public class SslCheckRepository : ISslCheckRepository
{
    private readonly DbConnectionFactory _factory;

    public SslCheckRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> CreateAsync(SslCertificateCheck check)
    {
        if (string.IsNullOrEmpty(check.Id))
            check.Id = Guid.NewGuid().ToString();

        const string sql = """
            INSERT INTO SslCertificateChecks (Id, MonitorId, CheckedAt, ExpiryDate, DaysRemaining, IsValid, ErrorMessage, CommonName, Issuer)
            VALUES (@Id, @MonitorId, @CheckedAt, @ExpiryDate, @DaysRemaining, @IsValid, @ErrorMessage, @CommonName, @Issuer)
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            check.Id,
            check.MonitorId,
            CheckedAt = check.CheckedAt.ToString("o"),
            ExpiryDate = check.ExpiryDate?.ToString("o"),
            check.DaysRemaining,
            IsValid = check.IsValid ? 1 : 0,
            check.ErrorMessage,
            check.CommonName,
            check.Issuer
        });
        return check.Id;
    }

    public async Task<SslCertificateCheck?> GetLatestByMonitorIdAsync(string monitorId)
    {
        using var conn = _factory.CreateConnection();
        var d = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM SslCertificateChecks WHERE MonitorId = @MonitorId ORDER BY CheckedAt DESC LIMIT 1",
            new { MonitorId = monitorId });
        return d == null ? null : MapCheck(d);
    }

    public async Task<IEnumerable<SslCertificateCheck>> GetByMonitorIdAsync(string monitorId, int limit = 50)
    {
        using var conn = _factory.CreateConnection();
        var results = await conn.QueryAsync<dynamic>(
            "SELECT * FROM SslCertificateChecks WHERE MonitorId = @MonitorId ORDER BY CheckedAt DESC LIMIT @Limit",
            new { MonitorId = monitorId, Limit = limit });
        return results.Select(MapCheck);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM SslCertificateChecks WHERE CheckedAt < @Cutoff",
            new { Cutoff = cutoff.ToString("o") });
    }

    private static SslCertificateCheck MapCheck(dynamic d) => new()
    {
        Id = d.Id,
        MonitorId = d.MonitorId,
        CheckedAt = DateTime.Parse((string)d.CheckedAt).ToUniversalTime(),
        ExpiryDate = d.ExpiryDate != null ? DateTime.Parse((string)d.ExpiryDate).ToUniversalTime() : null,
        DaysRemaining = d.DaysRemaining != null ? Convert.ToInt32(d.DaysRemaining) : null,
        IsValid = Convert.ToInt32(d.IsValid) == 1,
        ErrorMessage = d.ErrorMessage,
        CommonName = d.CommonName,
        Issuer = d.Issuer
    };
}
