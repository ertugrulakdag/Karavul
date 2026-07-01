namespace Karavul.Core.Entities;

public class MonitorCheckHeader
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MonitorCheckId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
