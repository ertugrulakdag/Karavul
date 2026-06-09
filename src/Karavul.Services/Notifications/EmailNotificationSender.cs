using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Karavul.Services.Notifications;

public class EmailNotificationSender : INotificationSender
{
    private readonly ILogger<EmailNotificationSender> _logger;
    public NotificationType NotificationType => NotificationType.Email;

    public EmailNotificationSender(ILogger<EmailNotificationSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendAsync(string recipient, string subject, string message, CancellationToken ct = default)
    {
        // TODO: Gerçek email entegrasyonu (SMTP / SendGrid / vb.) eklenecek
        _logger.LogInformation("[MOCK EMAIL] To: {Recipient}, Subject: {Subject}", recipient, subject);
        return Task.FromResult(true);
    }
}
