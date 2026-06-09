namespace Karavul.Host.Configuration;

public class KaravulSettings
{
    public int WebPort { get; set; } = 6666;
    public string DatabasePath { get; set; } = @"C:\ProgramData\Karavul\KaravulStatusMonitor.db";
    public string LogPath { get; set; } = @"C:\ProgramData\Karavul\logs";
    public int RetentionDays { get; set; } = 30;
    public int CheckWorkerIntervalMs { get; set; } = 5000;
    public int CleanupIntervalHours { get; set; } = 24;
    public string Language { get; set; } = "tr";
    public TelegramSettings Telegram { get; set; } = new();
}

public class TelegramSettings
{
    public string BotToken { get; set; } = string.Empty;
}
