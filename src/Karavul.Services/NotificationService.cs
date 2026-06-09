using Karavul.Core.Entities;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Karavul.Services;

public class NotificationService
{
    private readonly IContactGroupRepository _groupRepo;
    private readonly INotificationLogRepository _logRepo;
    private readonly IIncidentRepository _incidentRepo;
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IContactGroupRepository groupRepo,
        INotificationLogRepository logRepo,
        IIncidentRepository incidentRepo,
        IEnumerable<INotificationSender> senders,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _groupRepo = groupRepo;
        _logRepo = logRepo;
        _incidentRepo = incidentRepo;
        _senders = senders;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ProcessIncidentNotificationsAsync(Incident incident, MonitorTarget monitor, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(monitor.ContactGroupId)) return;

        var group = await _groupRepo.GetByIdAsync(monitor.ContactGroupId);
        if (group == null || !group.IsActive) return;

        bool shouldSend;

        if (incident.NotificationCount == 0)
        {
            shouldSend = true;
        }
        else if (group.RepeatAlertMinutes == 0)
        {
            shouldSend = false;
        }
        else
        {
            var nextNotificationTime = incident.LastNotificationAt!.Value
                .AddMinutes(group.RepeatAlertMinutes);
            shouldSend = DateTime.UtcNow >= nextNotificationTime;
        }

        if (!shouldSend) return;

        var code = incident.Code;
        var lang = _configuration["Karavul:Language"] ?? "tr";
        var subject = lang == "en" 
            ? $"🔴 [KARAVUL ALARM] {monitor.Name} - {incident.Reason}"
            : $"🔴 [KARAVUL ALARM] {monitor.Name} - {incident.Reason}";
        var message = lang == "en"
            ? $"""
            Code: #{code}
            Monitor: {monitor.Name}
            Status: DOWN
            Reason: {incident.Reason}
            Error: {incident.LastErrorMessage ?? "Unknown"}
            Started At: {incident.StartedAt.ToLocalTime():dd.MM.yyyy HH:mm:ss}
            """
            : $"""
            Kod: #{code}
            Monitor: {monitor.Name}
            Durum: DOWN
            Sebep: {incident.Reason}
            Hata: {incident.LastErrorMessage ?? "Bilinmiyor"}
            Başlangıç: {incident.StartedAt.ToLocalTime():dd.MM.yyyy HH:mm:ss}
            """;

        if (group.ActiveNotificationTypes.HasFlag(NotificationType.Email))
        {
            foreach (var email in group.Emails)
            {
                await SendNotificationAsync(incident, monitor, group,
                    NotificationType.Email, email.Email, subject, message, ct);
            }
        }

        if (group.ActiveNotificationTypes.HasFlag(NotificationType.Sms))
        {
            foreach (var phone in group.Phones)
            {
                var smsMessage = $"KARAVUL: {monitor.Name} DOWN - {incident.Reason}";
                await SendNotificationAsync(incident, monitor, group,
                    NotificationType.Sms, phone.PhoneNumber, subject, smsMessage, ct);
            }
        }

        if (group.ActiveNotificationTypes.HasFlag(NotificationType.Telegram))
        {
            foreach (var telegram in group.Telegrams)
            {
                await SendNotificationAsync(incident, monitor, group,
                    NotificationType.Telegram, telegram.ChatId, subject, message, ct);
            }
        }

        incident.LastNotificationAt = DateTime.UtcNow;
        incident.NotificationCount++;
        await _incidentRepo.UpdateAsync(incident);

        _logger.LogInformation("Bildirimler gönderildi. Monitor={MonitorName}, Count={Count}",
            monitor.Name, incident.NotificationCount);
    }

    public async Task ProcessRecoveryNotificationsAsync(Incident incident, MonitorTarget monitor, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(monitor.ContactGroupId)) return;

        var group = await _groupRepo.GetByIdAsync(monitor.ContactGroupId);
        if (group == null || !group.IsActive) return;

        var code = incident.Code;
        var lang = _configuration["Karavul:Language"] ?? "tr";
        var subject = lang == "en" 
            ? $"🟢 [KARAVUL ALARM] {monitor.Name} - Resolved"
            : $"🟢 [KARAVUL ALARM] {monitor.Name} - Sorun Çözüldü";
        var duration = incident.ResolvedAt.HasValue
            ? (incident.ResolvedAt.Value - incident.StartedAt).ToString(@"hh\:mm\:ss")
            : (lang == "en" ? "unknown" : "bilinmiyor");
            
        var message = lang == "en" 
            ? $"""
            Recovery Notification
            Code: #{code}
            Monitor: {monitor.Name}
            Status: UP
            Downtime: {duration}
            Resolved At: {incident.ResolvedAt?.ToLocalTime():dd.MM.yyyy HH:mm:ss}
            """
            : $"""
            İyileşme Bildirimi
            Kod: #{code}
            Monitor: {monitor.Name}
            Durum: UP
            Kesinti Süresi: {duration}
            Çözüm Zamanı: {incident.ResolvedAt?.ToLocalTime():dd.MM.yyyy HH:mm:ss}
            """;

        if (group.ActiveNotificationTypes.HasFlag(NotificationType.Email))
        {
            foreach (var email in group.Emails)
                await SendNotificationAsync(incident, monitor, group,
                    NotificationType.Email, email.Email, subject, message, ct);
        }

        if (group.ActiveNotificationTypes.HasFlag(NotificationType.Sms))
        {
            foreach (var phone in group.Phones)
                await SendNotificationAsync(incident, monitor, group,
                    NotificationType.Sms, phone.PhoneNumber, subject, message, ct);
        }

        if (group.ActiveNotificationTypes.HasFlag(NotificationType.Telegram))
        {
            foreach (var telegram in group.Telegrams)
            {
                await SendNotificationAsync(incident, monitor, group,
                    NotificationType.Telegram, telegram.ChatId, subject, message, ct);
            }
        }
    }

    private async Task SendNotificationAsync(
        Incident incident, MonitorTarget monitor, ContactGroup group,
        NotificationType type, string recipient, string subject, string message,
        CancellationToken ct)
    {
        var sender = _senders.FirstOrDefault(s => s.NotificationType == type);
        bool success = false;
        string? errorMessage = null;

        if (sender != null)
        {
            try
            {
                success = await sender.SendAsync(recipient, subject, message, ct);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.LogError(ex, "Bildirim gönderilemedi: {Type} → {Recipient}", type, recipient);
            }
        }
        else
        {
            errorMessage = $"No sender registered for {type}";
        }

        var log = new NotificationLog
        {
            IncidentId = incident.Id,
            MonitorId = monitor.Id,
            ContactGroupId = group.Id,
            NotificationType = type,
            Recipient = recipient,
            Subject = subject,
            Message = message,
            IsSuccess = success,
            ErrorMessage = errorMessage,
            SentAt = DateTime.UtcNow
        };

        await _logRepo.CreateAsync(log);
    }
}
