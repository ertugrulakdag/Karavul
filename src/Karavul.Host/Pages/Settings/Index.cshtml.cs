using Karavul.Core.Interfaces;
using Karavul.Host.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages.Settings;

public class IndexModel : PageModel
{
    private readonly IAppSettingRepository _settingRepo;
    private readonly KaravulSettings _settings;

    public IndexModel(IAppSettingRepository settingRepo, KaravulSettings settings)
    {
        _settingRepo = settingRepo;
        _settings = settings;
    }

    [BindProperty] public int RetentionDays { get; set; }
    [BindProperty] public int CheckWorkerIntervalMs { get; set; }
    public int WebPort => _settings.WebPort;
    public string DatabasePath => _settings.DatabasePath;
    public string LogPath => _settings.LogPath;
    public string AppVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Bilinmiyor";
    public string Language => _settings.Language;
    
    public string TelegramBotToken => string.IsNullOrEmpty(_settings.Telegram?.BotToken) ? "Ayarlanmamış" : _settings.Telegram.BotToken;

    public void OnGet()
    {
        RetentionDays = _settings.RetentionDays;
        CheckWorkerIntervalMs = _settings.CheckWorkerIntervalMs;
    }

    public IActionResult OnPost()
    {
        // In MVP, runtime settings are read from appsettings.json
        // For now, just show a message
        TempData["Success"] = "Ayarlar not edildi. Değişiklik için appsettings.Production.json dosyasını güncelleyin ve servisi yeniden başlatın.";
        return RedirectToPage();
    }
}
