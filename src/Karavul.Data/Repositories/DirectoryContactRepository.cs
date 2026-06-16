using Dapper;
using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;

namespace Karavul.Data.Repositories;

public class DirectoryContactRepository : IDirectoryContactRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public DirectoryContactRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DirectoryContact>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<DirectoryContact>(
            "SELECT * FROM DirectoryContacts ORDER BY FirstName, LastName");
    }

    public async Task<DirectoryContact?> GetByIdAsync(string id)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<DirectoryContact>(
            "SELECT * FROM DirectoryContacts WHERE Id = @Id", new { Id = id });
    }

    public async Task CreateAsync(DirectoryContact contact)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = """
            INSERT INTO DirectoryContacts (Id, FirstName, LastName, Email, PhoneNumber, TelegramChatId, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
            VALUES (@Id, @FirstName, @LastName, @Email, @PhoneNumber, @TelegramChatId, @IsActive, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy)
            """;
        await conn.ExecuteAsync(sql, contact);
    }

    public async Task UpdateAsync(DirectoryContact contact)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = """
            UPDATE DirectoryContacts 
            SET FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                PhoneNumber = @PhoneNumber,
                TelegramChatId = @TelegramChatId,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, contact);
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM DirectoryContacts WHERE Id = @Id", new { Id = id });
    }
}
