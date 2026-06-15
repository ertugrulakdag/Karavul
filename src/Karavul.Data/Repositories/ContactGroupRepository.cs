using Dapper;
using Karavul.Core.Entities;
using Karavul.Core.Interfaces;
using Karavul.Data.Database;

namespace Karavul.Data.Repositories;

public class ContactGroupRepository : IContactGroupRepository
{
    private readonly DbConnectionFactory _factory;

    public ContactGroupRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<ContactGroup>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        var groups = (await conn.QueryAsync<ContactGroup>(
            "SELECT * FROM ContactGroups ORDER BY Name")).ToList();

        if (!groups.Any()) return groups;

        var groupIds = groups.Select(g => g.Id).ToList();
        var members = await conn.QueryAsync<ContactGroupMember>(
            "SELECT * FROM ContactGroupMembers WHERE ContactGroupId IN @Ids",
            new { Ids = groupIds });

        foreach (var group in groups)
        {
            group.Members = members.Where(m => m.ContactGroupId == group.Id).ToList();
        }

        return groups;
    }

    public async Task<ContactGroup?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        var group = await conn.QueryFirstOrDefaultAsync<ContactGroup>(
            "SELECT * FROM ContactGroups WHERE Id = @Id", new { Id = id });

        if (group == null) return null;

        group.Members = (await conn.QueryAsync<ContactGroupMember>(
            "SELECT * FROM ContactGroupMembers WHERE ContactGroupId = @Id", new { Id = id })).ToList();

        return group;
    }

    public async Task<string> CreateAsync(ContactGroup group)
    {
        if (string.IsNullOrEmpty(group.Id))
            group.Id = Guid.NewGuid().ToString();

        group.CreatedAt = DateTime.UtcNow;

        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "INSERT INTO ContactGroups (Id, Name, RepeatAlertMinutes, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, ActiveNotificationTypes) VALUES (@Id, @Name, @RepeatAlertMinutes, @IsActive, @CreatedAt, @CreatedBy, @UpdatedAt, @UpdatedBy, @ActiveNotificationTypes)",
            new { group.Id, group.Name, group.RepeatAlertMinutes, IsActive = group.IsActive ? 1 : 0, CreatedAt = group.CreatedAt.ToString("o"), group.CreatedBy, UpdatedAt = group.UpdatedAt.ToString("o"), group.UpdatedBy, ActiveNotificationTypes = (int)group.ActiveNotificationTypes }, tx);

        foreach (var member in group.Members)
        {
            if (string.IsNullOrEmpty(member.Id))
                member.Id = Guid.NewGuid().ToString();
            member.ContactGroupId = group.Id;
            await conn.ExecuteAsync(
                "INSERT INTO ContactGroupMembers (Id, ContactGroupId, DirectoryContactId, FirstName, LastName, Email, PhoneNumber, TelegramChatId) VALUES (@Id, @ContactGroupId, @DirectoryContactId, @FirstName, @LastName, @Email, @PhoneNumber, @TelegramChatId)",
                member, tx);
        }

        await tx.CommitAsync();
        return group.Id;
    }

    public async Task UpdateAsync(ContactGroup group)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "UPDATE ContactGroups SET Name = @Name, RepeatAlertMinutes = @RepeatAlertMinutes, IsActive = @IsActive, ActiveNotificationTypes = @ActiveNotificationTypes, UpdatedAt = @UpdatedAt, UpdatedBy = @UpdatedBy WHERE Id = @Id",
            new { group.Id, group.Name, group.RepeatAlertMinutes, IsActive = group.IsActive ? 1 : 0, ActiveNotificationTypes = (int)group.ActiveNotificationTypes, UpdatedAt = group.UpdatedAt.ToString("o"), group.UpdatedBy }, tx);

        await conn.ExecuteAsync("DELETE FROM ContactGroupMembers WHERE ContactGroupId = @Id", new { Id = group.Id }, tx);

        foreach (var member in group.Members)
        {
            if (string.IsNullOrEmpty(member.Id))
                member.Id = Guid.NewGuid().ToString();
            member.ContactGroupId = group.Id;
            await conn.ExecuteAsync(
                "INSERT INTO ContactGroupMembers (Id, ContactGroupId, DirectoryContactId, FirstName, LastName, Email, PhoneNumber, TelegramChatId) VALUES (@Id, @ContactGroupId, @DirectoryContactId, @FirstName, @LastName, @Email, @PhoneNumber, @TelegramChatId)",
                member, tx);
        }

        await tx.CommitAsync();
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM ContactGroups WHERE Id = @Id", new { Id = id });
    }
}
