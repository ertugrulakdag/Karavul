using System;

namespace Karavul.Core.Enums;

[Flags]
public enum NotificationType
{
    None = 0,
    Email = 1 << 0,          // 1
    Sms = 1 << 1,            // 2
    PushNotification = 1 << 2, // 4
    Telegram = 1 << 3        // 8
}
