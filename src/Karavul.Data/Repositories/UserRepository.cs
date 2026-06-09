using Dapper;
using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;

namespace Karavul.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _factory;

    public UserRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var conn = _factory.CreateConnection();
        var d = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM Users WHERE Username = @Username COLLATE NOCASE",
            new { Username = username });
        return d == null ? null : MapUser(d);
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        var d = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
        return d == null ? null : MapUser(d);
    }

    public async Task<string> CreateAsync(User user)
    {
        if (string.IsNullOrEmpty(user.Id))
            user.Id = Guid.NewGuid().ToString();

        user.CreatedAt = DateTime.UtcNow;

        const string sql = """
            INSERT INTO Users (Id, Username, PasswordHash, IsPasswordChangeRequired, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, Role)
            VALUES (@Id, @Username, @PasswordHash, @IsPasswordChangeRequired, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy, @Role)
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            user.Id,
            user.Username,
            user.PasswordHash,
            IsPasswordChangeRequired = user.IsPasswordChangeRequired ? 1 : 0,
            CreatedAt = user.CreatedAt.ToString("o"),
            CreatedBy = user.CreatedBy,
            UpdatedAt = user.UpdatedAt.ToString("o"),
            UpdatedBy = user.UpdatedBy,
            Role = (int)user.Role
        });
        return user.Id;
    }

    public async Task UpdateAsync(User user)
    {
        const string sql = """
            UPDATE Users SET
                PasswordHash = @PasswordHash,
                IsPasswordChangeRequired = @IsPasswordChangeRequired,
                LastLoginAt = @LastLoginAt,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy,
                Role = @Role
            WHERE Id = @Id
            """;

        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync(sql, new
        {
            user.Id,
            user.PasswordHash,
            IsPasswordChangeRequired = user.IsPasswordChangeRequired ? 1 : 0,
            LastLoginAt = user.LastLoginAt?.ToString("o"),
            UpdatedAt = user.UpdatedAt.ToString("o"),
            UpdatedBy = user.UpdatedBy,
            Role = (int)user.Role
        });
    }

    public async Task<bool> AnyAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Users") > 0;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        var d = await conn.QueryAsync<dynamic>("SELECT * FROM Users ORDER BY Username");
        return d.Select(MapUser);
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Users WHERE Id = @Id", new { Id = id });
    }

    private static User MapUser(dynamic d) => new()
    {
        Id = d.Id,
        Username = d.Username,
        PasswordHash = d.PasswordHash,
        IsPasswordChangeRequired = Convert.ToInt32(d.IsPasswordChangeRequired) == 1,
        Role = (Karavul.Core.Enums.UserRole)Convert.ToInt32(d.Role),
        CreatedAt = DateTime.Parse((string)d.CreatedAt).ToUniversalTime(),
        CreatedBy = d.CreatedBy,
        UpdatedAt = d.UpdatedAt != null ? DateTime.Parse((string)d.UpdatedAt).ToUniversalTime() : DateTime.Parse((string)d.CreatedAt).ToUniversalTime(),
        UpdatedBy = d.UpdatedBy,
        LastLoginAt = d.LastLoginAt != null ? DateTime.Parse((string)d.LastLoginAt).ToUniversalTime() : null
    };
}
