using Karavul.Core.DTOs;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Karavul.Host.Pages;

public class IndexModel : PageModel
{
    private readonly IMonitorRepository _monitorRepo;
    private readonly IMonitorCheckRepository _checkRepo;
    private readonly IIncidentRepository _incidentRepo;
    private readonly IContactGroupRepository _groupRepo;

    public IndexModel(
        IMonitorRepository monitorRepo,
        IMonitorCheckRepository checkRepo,
        IIncidentRepository incidentRepo,
        IContactGroupRepository groupRepo)
    {
        _monitorRepo = monitorRepo;
        _checkRepo = checkRepo;
        _incidentRepo = incidentRepo;
        _groupRepo = groupRepo;
    }

    public DashboardDto Dashboard { get; set; } = new();

    public async Task OnGetAsync(int limit = 10)
    {
        var monitors = (await _monitorRepo.GetAllAsync()).ToList();
        var groups = (await _groupRepo.GetAllAsync()).ToList();
        var since24h = DateTime.UtcNow.AddHours(-24);

        var summaries = new List<MonitorSummaryDto>();
        double totalUptime = 0;
        double totalResponseTime = 0;
        int responsiveMonitors = 0;

        foreach (var m in monitors)
        {
            var groupName = groups.FirstOrDefault(g => g.Id == m.ContactGroupId)?.Name;
            var uptime = await _checkRepo.GetUptimePercentageAsync(m.Id, since24h);
            var avgResp = await _checkRepo.GetAverageResponseTimeAsync(m.Id, since24h);

            totalUptime += uptime;
            if (avgResp > 0) { totalResponseTime += avgResp; responsiveMonitors++; }

            summaries.Add(new MonitorSummaryDto
            {
                Id = m.Id,
                Name = m.Name,
                Url = m.Url,
                CurrentStatus = m.CurrentStatus,
                LastCheckedAt = m.LastCheckedAt,
                LastStatusCode = m.LastStatusCode,
                LastResponseTimeMs = m.LastResponseTimeMs,
                LastErrorMessage = m.LastErrorMessage,
                ContactGroupName = groupName,
                IsActive = m.IsActive,
                UptimePercent24h = uptime
            });
        }

        Dashboard = new DashboardDto
        {
            TotalMonitors = monitors.Count,
            UpMonitors = monitors.Count(m => m.CurrentStatus == MonitorStatus.Up),
            DownMonitors = monitors.Count(m => m.CurrentStatus == MonitorStatus.Down),
            WarningMonitors = monitors.Count(m => m.CurrentStatus == MonitorStatus.Warning),
            PausedMonitors = monitors.Count(m => !m.IsActive || m.CurrentStatus == MonitorStatus.Paused),
            ActiveIncidents = await _incidentRepo.GetActiveCountAsync(),
            Last24hUptimePercent = monitors.Any() ? Math.Round(totalUptime / monitors.Count, 2) : 100.0,
            AvgResponseTimeMs = responsiveMonitors > 0 ? Math.Round(totalResponseTime / responsiveMonitors, 0) : 0,
            Monitors = summaries
                .OrderBy(m => m.CurrentStatus == MonitorStatus.Down ? 0 : (m.CurrentStatus == MonitorStatus.Warning ? 1 : 2))
                .ThenBy(m => m.UptimePercent24h)
                .Take(limit)
                .ToList()
        };
    }

    public async Task<IActionResult> OnGetChartDataAsync(string period = "day")
    {
        DateTime since;
        string groupByFormat;
        int buckets;
        TimeSpan bucketSize;

        switch (period?.ToLower())
        {
            case "week":
                since = DateTime.UtcNow.AddDays(-6).Date; // 7 days including today
                groupByFormat = "%Y-%m-%d";
                buckets = 7;
                bucketSize = TimeSpan.FromDays(1);
                break;
            case "month":
                since = DateTime.UtcNow.AddDays(-29).Date; // 30 days including today
                groupByFormat = "%Y-%m-%d";
                buckets = 30;
                bucketSize = TimeSpan.FromDays(1);
                break;
            case "day":
            default:
                since = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0, DateTimeKind.Utc).AddHours(-23);
                groupByFormat = "%Y-%m-%d %H:00";
                buckets = 24;
                bucketSize = TimeSpan.FromHours(1);
                break;
        }

        var history = await _checkRepo.GetStatusHistoryAsync(since, groupByFormat);
        var historyDict = history.ToDictionary(h => (string)h.TimeGroup, h => h);
        
        var labels = new List<string>();
        var successData = new List<int>();
        var failData = new List<int>();
        
        for (int i = 0; i < buckets; i++)
        {
            DateTime currentBucket = since.Add(bucketSize * i);
            string key = period == "day" || string.IsNullOrEmpty(period)
                ? currentBucket.ToString("yyyy-MM-dd HH:00")
                : currentBucket.ToString("yyyy-MM-dd");

            if (period == "day" || string.IsNullOrEmpty(period))
            {
                labels.Add(currentBucket.AddHours(1).ToLocalTime().ToString("HH:00"));
            }
            else
            {
                labels.Add(currentBucket.ToLocalTime().ToString("dd MMM"));
            }

            if (historyDict.TryGetValue(key, out var h))
            {
                successData.Add(Convert.ToInt32(h.SuccessCount ?? 0));
                failData.Add(Convert.ToInt32(h.FailCount ?? 0));
            }
            else
            {
                successData.Add(0);
                failData.Add(0);
            }
        }

        return new JsonResult(new { labels, successData, failData });
    }

    public async Task<IActionResult> OnGetStatsAsync(string period = "day", int limit = 10)
    {
        await OnGetAsync(limit);
        var chartResult = await OnGetChartDataAsync(period) as JsonResult;
        return new JsonResult(new { stats = Dashboard, chartData = chartResult?.Value });
    }
}
