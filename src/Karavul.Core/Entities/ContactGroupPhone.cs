namespace Karavul.Core.Entities;

public class ContactGroupPhone
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ContactGroupId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
