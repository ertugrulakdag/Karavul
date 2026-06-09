namespace Karavul.Core.Entities;

public class ContactGroupTelegram
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ContactGroupId { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
}
