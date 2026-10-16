using System.Net;
using System.Net.Http.Json;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Karavul.Services.Notifications;

public class TelegramNotificationSender : INotificationSender
{
    private readonly ILogger<TelegramNotificationSender> _logger;
    private readonly string _botToken;
    private static readonly HttpClient SharedHttpClient = new();
    private readonly HttpClient _httpClient;
    
    public NotificationType NotificationType => NotificationType.Telegram;

    public TelegramNotificationSender(
        ILogger<TelegramNotificationSender> logger, 
        string botToken) : this(logger, botToken, SharedHttpClient)
    {
    }

    public TelegramNotificationSender(ILogger<TelegramNotificationSender> logger, string botToken, HttpClient httpClient)
    {
        _logger = logger;
        _botToken = botToken;
        _httpClient = httpClient;
    }

    public async Task<bool> SendAsync(string recipient, string subject, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("Telegram BotToken veya ChatId (recipient) ayarlanmamış. [MOCK] Telegram mesajı: {Message}", message);
            return true;
        }

        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            
            // Split before HTML encoding: Telegram limits the text after entity parsing.
            var plainText = $"{subject}\n{message}";
            bool firstChunk = true;
            foreach (var chunk in SplitMessage(plainText))
            { 
                var text = firstChunk && chunk.Length > subject.Length
                    ? $"<b>{WebUtility.HtmlEncode(subject)}</b>\n{WebUtility.HtmlEncode(chunk[(subject.Length + 1)..])}"
                    : WebUtility.HtmlEncode(chunk);
                var payload = new { chat_id = recipient, text, parse_mode = "HTML" };
                using var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError("Telegram mesajı gönderilemedi. Status: {Status}, Error: {Error}", response.StatusCode, errorResponse);
                    return false;
                }
                firstChunk = false;
            }

            _logger.LogInformation("Telegram mesajı başarıyla gönderildi. ChatId: {ChatId}", recipient);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram API çağrısında hata oluştu.");
            return false;
        }
    }

    private static IEnumerable<string> SplitMessage(string text)
    {
        for (int offset = 0; offset < text.Length;)
        {
            int length = Math.Min(4000, text.Length - offset);
            if (offset + length < text.Length && char.IsHighSurrogate(text[offset + length - 1])) length--;
            yield return text.Substring(offset, length);
            offset += length;
        }
    }
}
