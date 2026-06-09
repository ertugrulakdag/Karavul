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
        var emails = await conn.QueryAsync<ContactGroupEmail>(
            "SELECT * FROM ContactGroupEmails WHERE ContactGroupId IN @Ids",
            new { Ids = groupIds });
        var phones = await conn.QueryAsync<ContactGroupPhone>(
            "SELECT * FROM ContactGroupPhones WHERE ContactGroupId IN @Ids",
            new { Ids = groupIds });
        var telegrams = await conn.QueryAsync<ContactGroupTelegram>(
            "SELECT * FROM ContactGroupTelegrams WHERE ContactGroupId IN @Ids",
            new { Ids = groupIds });

        foreach (var group in groups)
        {
            group.Emails = emails.Where(e => e.ContactGroupId == group.Id).ToList();
            group.Phones = phones.Where(p => p.ContactGroupId == group.Id).ToList();
            group.Telegrams = telegrams.Where(t => t.ContactGroupId == group.Id).ToList();
        }

        return groups;
    }

    public async Task<ContactGroup?> GetByIdAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        var group = await conn.QueryFirstOrDefaultAsync<ContactGroup>(
            "SELECT * FROM ContactGroups WHERE Id = @Id", new { Id = id });

        if (group == null) return null;

        group.Emails = (await conn.QueryAsync<ContactGroupEmail>(
            "SELECT * FROM ContactGroupEmails WHERE ContactGroupId = @Id", new { Id = id })).ToList();
        group.Phones = (await conn.QueryAsync<ContactGroupPhone>(
            "SELECT * FROM ContactGroupPhones WHERE ContactGroupId = @Id", new { Id = id })).ToList();
        group.Telegrams = (await conn.QueryAsync<ContactGroupTelegram>(
            "SELECT * FROM ContactGroupTelegrams WHERE ContactGroupId = @Id", new { Id = id })).ToList();

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

        foreach (var email in group.Emails)
        {
            email.Id = Guid.NewGuid().ToString();
            email.ContactGroupId = group.Id;
            await conn.ExecuteAsync(
                "INSERT INTO ContactGroupEmails (Id, ContactGroupId, Email) VALUES (@Id, @ContactGroupId, @Email)",
                email, tx);
        }

        foreach (var phone in group.Phones)
        {
            phone.Id = Guid.NewGuid().ToString();
            phone.ContactGroupId = group.Id;
            await conn.ExecuteAsync(
                "INSERT INTO ContactGroupPhones (Id, ContactGroupId, PhoneNumber) VALUES (@Id, @ContactGroupId, @PhoneNumber)",
                phone, tx);
        }

        foreach (var telegram in group.Telegrams)
        {
            telegram.Id = Guid.NewGuid().ToString();
            telegram.ContactGroupId = group.Id;
            await conn.ExecuteAsync(
                "INSERT INTO ContactGroupTelegrams (Id, ContactGroupId, ChatId) VALUES (@Id, @ContactGroupId, @ChatId)",
                telegram, tx);
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

        await conn.ExecuteAsync("DELETE FROM ContactGroupEmails WHERE ContactGroupId = @Id", new { Id = group.Id }, tx);
        await conn.ExecuteAsync("DELETE FROM ContactGroupPhones WHERE ContactGroupId = @Id", new { Id = group.Id }, tx);
        await conn.ExecuteAsync("DELETE FROM ContactGroupTelegrams WHERE ContactGroupId = @Id", new { Id = group.Id }, tx);

        foreach (var email in group.Emails)
        {
            email.Id = Guid.NewGuid().ToString();
            email.ContactGroupId = group.Id;
            await conn.ExecuteAsync(
                "INSERT INTO ContactGroupEmails (Id, ContactGroupId, Email) VALUES (@Id, @ContactGroupId, @Email)",
                email, tx);
        }

        foreach (var phone in group.Phones)
        {
            phone.Id = Guid.NewGuid().ToString();
            phone.ContactGroupId = group.Id;
            await conn.ExecuteAsync(
                "INSERT INTO ContactGroupPhones (Id, ContactGroupId, PhoneNumber) VALUES (@Id, @ContactGroupId, @PhoneNumber)",
                phone, tx);
        }

        foreach (var telegram in group.Telegrams)
        {
            telegram.Id = Guid.NewGuid().ToString();
            telegram.ContactGroupId = group.Id;
            await conn.ExecuteAsync(
                "INSERT INTO ContactGroupTelegrams (Id, ContactGroupId, ChatId) VALUES (@Id, @ContactGroupId, @ChatId)",
                telegram, tx);
        }

        await tx.CommitAsync();
    }

    public async Task DeleteAsync(string id)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM ContactGroups WHERE Id = @Id", new { Id = id });
    }
}
