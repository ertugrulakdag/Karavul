using Karavul.Core.Entities;

namespace Karavul.Core.Interfaces;

public interface INotificationSender
{
    Task<bool> SendAsync(string recipient, string subject, string message, CancellationToken ct = default);
    Karavul.Core.Enums.NotificationType NotificationType { get; }
}
