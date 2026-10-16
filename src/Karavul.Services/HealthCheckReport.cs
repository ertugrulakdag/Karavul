using System.Text.Json;

namespace Karavul.Services;

internal sealed class HealthCheckReport
{
    public string? Status { get; private init; }
    public string? Version { get; private init; }
    public string? Environment { get; private init; }
    public string? Duration { get; private init; }
    public string? Description { get; private init; }
    public List<Component> Components { get; } = [];

    public bool IsUnhealthy => IsStatus(Status, "Unhealthy") || Components.Any(c => IsStatus(c.Status, "Unhealthy"));
    public bool IsDegraded => !IsUnhealthy && (IsStatus(Status, "Degraded") || Components.Any(c => IsStatus(c.Status, "Degraded")));

    public static HealthCheckReport? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            var report = new HealthCheckReport
            {
                Status = Text(root, "status"),
                Version = Text(root, "version"),
                Environment = Text(root, "environment"),
                Duration = ReadDuration(root, "totalDurationMs", "totalDuration"),
                Description = Text(root, "description")
            };

            if (Property(root, "checks") is { ValueKind: JsonValueKind.Array } checks)
            {
                foreach (var check in checks.EnumerateArray())
                {
                    if (check.ValueKind == JsonValueKind.Object)
                        report.Components.Add(ReadComponent(check, Text(check, "name")));
                }
            }

            if (Property(root, "entries") is { ValueKind: JsonValueKind.Object } entries)
            {
                foreach (var entry in entries.EnumerateObject())
                {
                    if (entry.Value.ValueKind == JsonValueKind.Object)
                        report.Components.Add(ReadComponent(entry.Value, entry.Name));
                }
            }

            return report;
        }
        catch (JsonException)
        {
            // Other response formats remain available as raw text in the details dialog.
            return null;
        }
    }

    public string DescribeProblem(string language, int? httpStatusCode)
    {
        bool english = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase);
        var details = new List<string>
        {
            IsUnhealthy
                ? (english ? "Health check failed: Unhealthy." : "Sağlık kontrolü başarısız: Unhealthy.")
                : (english ? "Health check degraded: a component has performance or functionality issues." : "Sağlık kontrolü Degraded: bir bileşende performans veya işlev sorunu var.")
        };

        var summary = new List<string>();
        Add(summary, english ? "Overall status" : "Genel durum", Status);
        Add(summary, "HTTP", httpStatusCode?.ToString());
        Add(summary, english ? "Version" : "Sürüm", Version);
        Add(summary, english ? "Environment" : "Ortam", Environment);
        Add(summary, english ? "Total duration" : "Toplam süre", Duration);
        Add(summary, english ? "Description" : "Açıklama", Description);
        if (summary.Count > 0) details.Add(string.Join(" | ", summary));

        foreach (var component in Components.Where(c => IsStatus(c.Status, "Unhealthy") || IsStatus(c.Status, "Degraded")))
        {
            var fields = new List<string>();
            Add(fields, english ? "Component" : "Bileşen", component.Name);
            Add(fields, english ? "Status" : "Durum", component.Status);
            Add(fields, english ? "Duration" : "Süre", component.Duration);
            Add(fields, english ? "Description" : "Açıklama", component.Description);
            Add(fields, english ? "Tags" : "Etiketler", component.Tags);
            Add(fields, english ? "Data" : "Veri", component.Data);
            Add(fields, english ? "Exception" : "İstisna", component.Exception);
            details.Add(string.Join(" | ", fields));
        }

        return string.Join(System.Environment.NewLine, details);
    }

    private static Component ReadComponent(JsonElement value, string? name) => new(
        name, Text(value, "status"), ReadDuration(value, "durationMs", "duration"),
        Text(value, "description"), ReadTags(value), Data(value, "data"), Data(value, "exception"));

    private static string? ReadTags(JsonElement value)
    {
        if (Property(value, "tags") is not { ValueKind: JsonValueKind.Array } tags) return null;
        return string.Join(", ", tags.EnumerateArray()
            .Where(t => t.ValueKind == JsonValueKind.String)
            .Select(t => t.GetString()).Where(t => !string.IsNullOrWhiteSpace(t)));
    }

    private static string? ReadDuration(JsonElement value, string milliseconds, string duration)
    {
        var ms = Text(value, milliseconds);
        return ms != null ? $"{ms} ms" : Text(value, duration);
    }

    private static JsonElement? Property(JsonElement value, string name)
    {
        foreach (var property in value.EnumerateObject())
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return property.Value;
        return null;
    }

    private static string? Text(JsonElement value, string name) => Property(value, name) switch
    {
        { ValueKind: JsonValueKind.String } text => text.GetString()?.Trim(),
        { ValueKind: JsonValueKind.Number } number => number.GetRawText(),
        _ => null
    };

    private static string? Data(JsonElement value, string name) => Property(value, name) switch
    {
        { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
        { ValueKind: JsonValueKind.String } text => text.GetString(),
        { ValueKind: JsonValueKind.Object } data when !data.EnumerateObject().Any() => null,
        { } data => data.GetRawText(),
        _ => null
    };

    private static bool IsStatus(string? value, string status) => string.Equals(value, status, StringComparison.OrdinalIgnoreCase);

    private static void Add(List<string> fields, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) fields.Add($"{label}: {value}");
    }

    internal sealed record Component(string? Name, string? Status, string? Duration, string? Description, string? Tags, string? Data, string? Exception);
}
