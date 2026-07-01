using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Karavul.Data.Database;

public class SchemaInitializer
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger<SchemaInitializer> _logger;

    public SchemaInitializer(DbConnectionFactory connectionFactory, ILogger<SchemaInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        SqlMapper.AddTypeHandler(new UtcDateTimeHandler());
        SqlMapper.AddTypeHandler(new NullableUtcDateTimeHandler());

        _logger.LogInformation("Veritabanı şeması başlatılıyor...");

        using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync();

        // WAL mode for better concurrent performance
        await conn.ExecuteAsync("PRAGMA journal_mode=WAL;");
        await conn.ExecuteAsync("PRAGMA foreign_keys=ON;");

        await CreateTablesAsync(conn);
        await CreateIndexesAsync(conn);
        await MigrateDatabaseAsync(conn);

        _logger.LogInformation("Veritabanı şeması başarıyla başlatıldı.");
    }

    private static async Task CreateTablesAsync(SqliteConnection conn)
    {
        var sql = """
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT PRIMARY KEY,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                IsPasswordChangeRequired INTEGER NOT NULL DEFAULT 0,
                Role INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedBy TEXT,
                LastLoginAt TEXT
            );

            CREATE TABLE IF NOT EXISTS ContactGroups (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                RepeatAlertMinutes INTEGER NOT NULL DEFAULT 0,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedBy TEXT,
                ActiveNotificationTypes INTEGER NOT NULL DEFAULT 11
            );

            CREATE TABLE IF NOT EXISTS ContactGroupEmails (
                Id TEXT PRIMARY KEY,
                ContactGroupId TEXT NOT NULL,
                Email TEXT NOT NULL,
                FOREIGN KEY (ContactGroupId) REFERENCES ContactGroups(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ContactGroupPhones (
                Id TEXT PRIMARY KEY,
                ContactGroupId TEXT NOT NULL,
                PhoneNumber TEXT NOT NULL,
                FOREIGN KEY (ContactGroupId) REFERENCES ContactGroups(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS ContactGroupTelegrams (
                Id TEXT PRIMARY KEY,
                ContactGroupId TEXT NOT NULL,
                ChatId TEXT NOT NULL,
                FOREIGN KEY (ContactGroupId) REFERENCES ContactGroups(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS DirectoryContacts (
                Id TEXT PRIMARY KEY,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Email TEXT NOT NULL,
                PhoneNumber TEXT NOT NULL,
                TelegramChatId TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedBy TEXT
            );

            CREATE TABLE IF NOT EXISTS ContactGroupMembers (
                Id TEXT PRIMARY KEY,
                ContactGroupId TEXT NOT NULL,
                DirectoryContactId TEXT,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Email TEXT NOT NULL,
                PhoneNumber TEXT NOT NULL,
                TelegramChatId TEXT NOT NULL,
                FOREIGN KEY (ContactGroupId) REFERENCES ContactGroups(Id) ON DELETE CASCADE,
                FOREIGN KEY (DirectoryContactId) REFERENCES DirectoryContacts(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS Monitors (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Url TEXT NOT NULL,
                MonitorType INTEGER NOT NULL DEFAULT 0,
                HttpMethod TEXT NOT NULL DEFAULT 'GET',
                ExpectedStatusCode INTEGER NOT NULL DEFAULT 200,
                CheckIntervalSeconds INTEGER NOT NULL DEFAULT 60,
                TimeoutSeconds INTEGER NOT NULL DEFAULT 30,
                MaxResponseTimeMs INTEGER NOT NULL DEFAULT 5000,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CheckSsl INTEGER NOT NULL DEFAULT 0,
                SslWarningDays INTEGER NOT NULL DEFAULT 30,
                IsHealthJson INTEGER NOT NULL DEFAULT 0,
                ContactGroupId TEXT,
                Description TEXT,
                CurrentStatus INTEGER NOT NULL DEFAULT 0,
                LastCheckedAt TEXT,
                LastStatusCode INTEGER,
                LastResponseTimeMs INTEGER,
                LastErrorMessage TEXT,
                TriggerRate INTEGER NOT NULL DEFAULT 60,
                IsInTriggerProcess INTEGER NOT NULL DEFAULT 0,
                TriggerProcessStartedAt TEXT,
                TriggerProcessFailCount INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                UpdatedAt TEXT NOT NULL,
                UpdatedBy TEXT,
                FOREIGN KEY (ContactGroupId) REFERENCES ContactGroups(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS MonitorChecks (
                Id TEXT PRIMARY KEY,
                MonitorId TEXT NOT NULL,
                CheckedAt TEXT NOT NULL,
                IsSuccess INTEGER NOT NULL,
                StatusCode INTEGER,
                ResponseTimeMs INTEGER NOT NULL DEFAULT 0,
                ErrorMessage TEXT,
                CheckResultType INTEGER NOT NULL DEFAULT 0,
                HealthJson TEXT,
                FOREIGN KEY (MonitorId) REFERENCES Monitors(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS MonitorCheckHeaders (
                Id TEXT PRIMARY KEY,
                MonitorCheckId TEXT NOT NULL,
                Name TEXT NOT NULL,
                Value TEXT NOT NULL,
                FOREIGN KEY (MonitorCheckId) REFERENCES MonitorChecks(Id) ON DELETE CASCADE
            );


            CREATE TABLE IF NOT EXISTS Incidents (
                Id TEXT PRIMARY KEY,
                MonitorId TEXT NOT NULL,
                StartedAt TEXT NOT NULL,
                ResolvedAt TEXT,
                Status INTEGER NOT NULL DEFAULT 0,
                Reason TEXT NOT NULL,
                LastErrorMessage TEXT,
                LastNotificationAt TEXT,
                NotificationCount INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                ResolvedBy TEXT,
                Code TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (MonitorId) REFERENCES Monitors(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS NotificationLogs (
                Id TEXT PRIMARY KEY,
                IncidentId TEXT NOT NULL,
                MonitorId TEXT NOT NULL,
                ContactGroupId TEXT,
                NotificationType INTEGER NOT NULL DEFAULT 0,
                Recipient TEXT NOT NULL,
                Subject TEXT NOT NULL,
                Message TEXT NOT NULL,
                IsSuccess INTEGER NOT NULL DEFAULT 0,
                ErrorMessage TEXT,
                SentAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS SslCertificateChecks (
                Id TEXT PRIMARY KEY,
                MonitorId TEXT NOT NULL,
                CheckedAt TEXT NOT NULL,
                ExpiryDate TEXT,
                DaysRemaining INTEGER,
                IsValid INTEGER NOT NULL DEFAULT 0,
                ErrorMessage TEXT,
                CommonName TEXT,
                Issuer TEXT,
                FOREIGN KEY (MonitorId) REFERENCES Monitors(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS AppSettings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
            """;

        await conn.ExecuteAsync(sql);
    }

    private static async Task CreateIndexesAsync(SqliteConnection conn)
    {
        var sql = """
            CREATE INDEX IF NOT EXISTS IX_MonitorChecks_MonitorId_CheckedAt 
                ON MonitorChecks (MonitorId, CheckedAt DESC);

            CREATE INDEX IF NOT EXISTS IX_MonitorCheckHeaders_MonitorCheckId 
                ON MonitorCheckHeaders (MonitorCheckId);


            CREATE INDEX IF NOT EXISTS IX_Incidents_MonitorId_Status 
                ON Incidents (MonitorId, Status);

            CREATE INDEX IF NOT EXISTS IX_NotificationLogs_IncidentId_SentAt 
                ON NotificationLogs (IncidentId, SentAt DESC);

            CREATE INDEX IF NOT EXISTS IX_Monitors_IsActive 
                ON Monitors (IsActive);

            CREATE INDEX IF NOT EXISTS IX_SslChecks_MonitorId_CheckedAt
                ON SslCertificateChecks (MonitorId, CheckedAt DESC);
            """;

        await conn.ExecuteAsync(sql);
    }

    private static async Task MigrateDatabaseAsync(SqliteConnection conn)
    {
        try
        {
            await conn.ExecuteAsync("ALTER TABLE Users ADD COLUMN Role INTEGER NOT NULL DEFAULT 1;");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }

        try
        {
            await conn.ExecuteAsync("ALTER TABLE Monitors ADD COLUMN IsHealthJson INTEGER NOT NULL DEFAULT 0;");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }

        try
        {
            await conn.ExecuteAsync("ALTER TABLE MonitorChecks ADD COLUMN HealthJson TEXT;");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }

        try
        {
            await conn.ExecuteAsync("ALTER TABLE ContactGroups ADD COLUMN ActiveNotificationTypes INTEGER NOT NULL DEFAULT 11;");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }

        try
        {
            await conn.ExecuteAsync("ALTER TABLE Incidents ADD COLUMN IsManuallyResolved INTEGER NOT NULL DEFAULT 0;");
        }
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }

        try { await conn.ExecuteAsync("ALTER TABLE Users ADD COLUMN CreatedBy TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        try { await conn.ExecuteAsync("ALTER TABLE Users ADD COLUMN UpdatedAt TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        try { await conn.ExecuteAsync("ALTER TABLE Users ADD COLUMN UpdatedBy TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }

        try { await conn.ExecuteAsync("ALTER TABLE ContactGroups ADD COLUMN CreatedBy TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        try { await conn.ExecuteAsync("ALTER TABLE ContactGroups ADD COLUMN UpdatedAt TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        try { await conn.ExecuteAsync("ALTER TABLE ContactGroups ADD COLUMN UpdatedBy TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }

        try { await conn.ExecuteAsync("ALTER TABLE Monitors ADD COLUMN CreatedBy TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        try { await conn.ExecuteAsync("ALTER TABLE Monitors ADD COLUMN UpdatedBy TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        try { await conn.ExecuteAsync("ALTER TABLE Monitors ADD COLUMN TriggerRate INTEGER NOT NULL DEFAULT 60;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        try { await conn.ExecuteAsync("ALTER TABLE Monitors ADD COLUMN IsInTriggerProcess INTEGER NOT NULL DEFAULT 0;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        try { await conn.ExecuteAsync("ALTER TABLE Monitors ADD COLUMN TriggerProcessStartedAt TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        try { await conn.ExecuteAsync("ALTER TABLE Monitors ADD COLUMN TriggerProcessFailCount INTEGER NOT NULL DEFAULT 0;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }

        try { await conn.ExecuteAsync("ALTER TABLE Incidents ADD COLUMN ResolvedBy TEXT;"); } catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }
        
        try 
        { 
            await conn.ExecuteAsync("ALTER TABLE Incidents ADD COLUMN Code TEXT NOT NULL DEFAULT '';"); 
            var oldIncidents = await conn.QueryAsync<string>("SELECT Id FROM Incidents WHERE Code = ''");
            foreach (var id in oldIncidents)
            {
                var tempGuid = Guid.TryParse(id, out var g) ? g : Guid.NewGuid();
                var encoded = Karavul.Core.Helpers.GuidEncoder.Encode(tempGuid).Replace("-", "").Replace("_", "");
                var code = encoded.Substring(0, Math.Min(8, encoded.Length));
                try {
                    await conn.ExecuteAsync("UPDATE Incidents SET Code = @Code WHERE Id = @Id", new { Code = code, Id = id });
                } catch { } // Ignore unique constraint conflicts on old records just in case
            }
            
            await conn.ExecuteAsync("CREATE UNIQUE INDEX IF NOT EXISTS IX_Incidents_Code ON Incidents (Code) WHERE Code != '';");
        } 
        catch (SqliteException ex) when (ex.Message.Contains("duplicate column name")) { }

        // Migration from old emails, phones, telegrams to members
        await MigrateContactGroupMembersAsync(conn);
    }

    private static async Task MigrateContactGroupMembersAsync(SqliteConnection conn)
    {
        var membersExist = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM ContactGroupMembers");
        if (membersExist > 0) return;

        var groups = await conn.QueryAsync<string>("SELECT Id FROM ContactGroups");
        foreach (var groupId in groups)
        {
            var emails = await conn.QueryAsync<string>("SELECT Email FROM ContactGroupEmails WHERE ContactGroupId = @Id", new { Id = groupId });
            var phones = await conn.QueryAsync<string>("SELECT PhoneNumber FROM ContactGroupPhones WHERE ContactGroupId = @Id", new { Id = groupId });
            var telegrams = await conn.QueryAsync<string>("SELECT ChatId FROM ContactGroupTelegrams WHERE ContactGroupId = @Id", new { Id = groupId });

            int i = 1;
            foreach (var email in emails)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO ContactGroupMembers (Id, ContactGroupId, FirstName, LastName, Email, PhoneNumber, TelegramChatId) VALUES (@Id, @ContactGroupId, @FirstName, @LastName, @Email, @PhoneNumber, @TelegramChatId)",
                    new { Id = Guid.NewGuid().ToString(), ContactGroupId = groupId, FirstName = $"Üye {i++} (E-posta)", LastName = "", Email = email, PhoneNumber = "", TelegramChatId = "" }
                );
            }
            foreach (var phone in phones)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO ContactGroupMembers (Id, ContactGroupId, FirstName, LastName, Email, PhoneNumber, TelegramChatId) VALUES (@Id, @ContactGroupId, @FirstName, @LastName, @Email, @PhoneNumber, @TelegramChatId)",
                    new { Id = Guid.NewGuid().ToString(), ContactGroupId = groupId, FirstName = $"Üye {i++} (Telefon)", LastName = "", Email = "", PhoneNumber = phone, TelegramChatId = "" }
                );
            }
            foreach (var telegram in telegrams)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO ContactGroupMembers (Id, ContactGroupId, FirstName, LastName, Email, PhoneNumber, TelegramChatId) VALUES (@Id, @ContactGroupId, @FirstName, @LastName, @Email, @PhoneNumber, @TelegramChatId)",
                    new { Id = Guid.NewGuid().ToString(), ContactGroupId = groupId, FirstName = $"Üye {i++} (Telegram)", LastName = "", Email = "", PhoneNumber = "", TelegramChatId = telegram }
                );
            }
        }
    }
}
