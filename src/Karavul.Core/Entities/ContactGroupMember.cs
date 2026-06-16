namespace Karavul.Core.Entities;

public class ContactGroupMember
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ContactGroupId { get; set; } = string.Empty;
    public string? DirectoryContactId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string TelegramChatId { get; set; } = string.Empty;
}
