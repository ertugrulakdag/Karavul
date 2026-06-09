using Dapper;
using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;

namespace Karavul.Data.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly DbConnectionFactory _factory;

    public AppSettingRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<string?> GetAsync(string key)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT Value FROM AppSettings WHERE Key = @Key", new { Key = key });
    }

    public async Task SetAsync(string key, string value)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO AppSettings (Key, Value) VALUES (@Key, @Value) ON CONFLICT(Key) DO UPDATE SET Value = @Value",
            new { Key = key, Value = value });
    }

    public async Task<IEnumerable<AppSetting>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<AppSetting>("SELECT * FROM AppSettings ORDER BY Key");
    }
}
