using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Karavul.Services.Notifications;

public class PushNotificationSender : INotificationSender
{
    private readonly ILogger<PushNotificationSender> _logger;
    public NotificationType NotificationType => NotificationType.PushNotification;

    public PushNotificationSender(ILogger<PushNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string recipient, string subject, string message, CancellationToken ct = default)
    {
        // TODO: Web Push / Firebase vb. entegrasyonu eklenecek
        _logger.LogInformation("[MOCK PUSH] To: {Recipient}, Subject: {Subject}", recipient, subject);
        return Task.FromResult(true);
    }
}
