namespace Karavul.Core.DTOs;

public class PaginationModel
{
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalRecords { get; set; } = 0;
    public string BaseUrl { get; set; } = string.Empty;
    public string QueryStringParam { get; set; } = "p";
    public Dictionary<string, string> RouteValues { get; set; } = new();

    public string GetUrl(int page)
    {
        var url = BaseUrl + "?" + QueryStringParam + "=" + page;
        foreach (var kvp in RouteValues)
        {
            url += $"&{kvp.Key}={kvp.Value}";
        }
        return url;
    }
}
