using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Karavul.Host.Localization;

public interface ILocalizationService
{
    string this[string key] { get; }
    string GetString(string key, string lang);
    string CurrentLanguage { get; }
}

public class LocalizationService : ILocalizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<string, Dictionary<string, string>> _dictionaries = new();
    private readonly ILogger<LocalizationService> _logger;

    public LocalizationService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, ILogger<LocalizationService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        
        LoadDictionary("tr", env);
        LoadDictionary("en", env);
    }

    private void LoadDictionary(string lang, IWebHostEnvironment env)
    {
        var filePath = Path.Combine(env.ContentRootPath, "Resources", $"{lang}.json");
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (dict != null)
                {
                    _dictionaries[lang] = dict;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load dictionary for language {Lang}", lang);
        }
        _dictionaries[lang] = new Dictionary<string, string>();
    }

    public string CurrentLanguage
    {
        get
        {
            var lang = _httpContextAccessor.HttpContext?.Request.Cookies["Karavul.Lang"] ?? "tr";
            if (lang != "tr" && lang != "en") lang = "tr";
            return lang;
        }
    }

    public string this[string key]
    {
        get
        {
            return GetString(key, CurrentLanguage);
        }
    }

    public string GetString(string key, string lang)
    {
        if (_dictionaries.TryGetValue(lang, out var dict))
        {
            if (dict.TryGetValue(key, out var val))
            {
                return val;
            }
        }
        return key; // return key if not found
    }
}
