using System.Net.Http.Json;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Karavul.Services.Notifications;

public class TelegramNotificationSender : INotificationSender
{
    private readonly ILogger<TelegramNotificationSender> _logger;
    private readonly string _botToken;
    private static readonly HttpClient _httpClient = new HttpClient();
    
    public NotificationType NotificationType => NotificationType.Telegram;

    public TelegramNotificationSender(
        ILogger<TelegramNotificationSender> logger, 
        string botToken)
    {
        _logger = logger;
        _botToken = botToken;
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
            
            // Format message
            var text = $"*{subject}*\n{message}";
            var payload = new 
            { 
                chat_id = recipient, 
                text = text, 
                parse_mode = "Markdown" 
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Telegram mesajı başarıyla gönderildi. ChatId: {ChatId}", recipient);
                return true;
            }
            
            var errorResponse = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Telegram mesajı gönderilemedi. Status: {Status}, Error: {Error}", response.StatusCode, errorResponse);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram API çağrısında hata oluştu.");
            return false;
        }
    }
}
