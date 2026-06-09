using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Karavul.Services.Notifications;

public class SmsNotificationSender : INotificationSender
{
    private readonly ILogger<SmsNotificationSender> _logger;
    public NotificationType NotificationType => NotificationType.Sms;

    public SmsNotificationSender(ILogger<SmsNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string recipient, string subject, string message, CancellationToken ct = default)
    {
        // TODO: Gerçek SMS entegrasyonu eklenecek
        _logger.LogInformation("[MOCK SMS] To: {Recipient}, Message: {Message}", recipient, message);
        return Task.FromResult(true);
    }
}
