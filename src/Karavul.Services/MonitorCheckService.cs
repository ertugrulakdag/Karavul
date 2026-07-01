using System.Security.Cryptography.X509Certificates;
using Karavul.Core.Entities;
using Karavul.Core.Enums;
using Karavul.Core.Interfaces;
using Microsoft.Extensions.Logging;
using MonitorTarget = Karavul.Core.Entities.MonitorTarget;

namespace Karavul.Services;

public class MonitorCheckService
{
    private readonly IMonitorCheckRepository _checkRepo;
    private readonly ISslCheckRepository _sslRepo;
    private readonly ILogger<MonitorCheckService> _logger;

    public MonitorCheckService(
        IMonitorCheckRepository checkRepo,
        ISslCheckRepository sslRepo,
        ILogger<MonitorCheckService> logger)
    {
        _checkRepo = checkRepo;
        _sslRepo = sslRepo;
        _logger = logger;
    }

    /// <summary>
    /// Engellenen URL şemaları – yalnızca http ve https izinlidir.
    /// file://, ftp://, dict://, gopher:// vb. SSRF vektörlerini kapatır.
    /// </summary>
    private static readonly HashSet<string> AllowedSchemes =
        new(StringComparer.OrdinalIgnoreCase) { "http", "https" };

    /// <summary>
    /// Kullanıcı tanımlı monitör URL'ini SSRF açısından doğrular.
    /// </summary>
    /// <exception cref="InvalidOperationException">Geçersiz URL veya engellenen hedef.</exception>
    private static void ValidateMonitorUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Geçersiz URL formatı: {url}");

        // Yalnızca http ve https şemalarına izin ver
        if (!AllowedSchemes.Contains(uri.Scheme))
            throw new InvalidOperationException(
                $"İzin verilmeyen URL şeması '{uri.Scheme}'. Yalnızca http ve https desteklenir.");

        // Hostname'i IP'ye çözümle ve tehlikeli aralıkları engelle
        var host = uri.Host;
        if (System.Net.IPAddress.TryParse(host, out var ip))
        {
            var bytes = ip.GetAddressBytes();

            // 169.254.x.x – AWS/Azure/GCP bulut meta veri endpoint'i (IMDS)
            if (bytes.Length == 4 && bytes[0] == 169 && bytes[1] == 254)
                throw new InvalidOperationException(
                    $"Engellenen IP aralığı (link-local/cloud metadata): {host}");
        }

        // Tehlikeli hostname'leri doğrudan engelle
        if (host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("169.254.169.254", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Engellenen bulut meta veri hedefi: {host}");
    }

    public async Task<MonitorCheck> CheckHttpAsync(MonitorTarget monitor, HttpClient httpClient, CancellationToken ct)
    {
        var check = new MonitorCheck
        {
            MonitorId = monitor.Id,
            CheckedAt = DateTime.UtcNow
        };

        // SSRF koruması: URL şeması ve hedef IP doğrulaması
        try
        {
            ValidateMonitorUrl(monitor.Url);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("SSRF engellendi – Monitor: {Name}, URL: {Url}, Sebep: {Reason}",
                monitor.Name, monitor.Url, ex.Message);

            check.IsSuccess = false;
            check.CheckResultType = CheckResultType.ConnectionError;
            check.ErrorMessage = $"Güvenlik: {ex.Message}";
            await _checkRepo.CreateAsync(check);
            return check;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(
                new HttpMethod(monitor.HttpMethod ?? "GET"),
                monitor.Url);

            using var response = await httpClient.SendAsync(request, ct);
            sw.Stop();

            check.ResponseTimeMs = sw.ElapsedMilliseconds;
            check.StatusCode = (int)response.StatusCode;

            // Extract headers
            foreach (var header in response.Headers)
            {
                check.Headers.Add(new MonitorCheckHeader
                {
                    MonitorCheckId = check.Id,
                    Name = header.Key,
                    Value = string.Join(", ", header.Value)
                });
            }
            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    check.Headers.Add(new MonitorCheckHeader
                    {
                        MonitorCheckId = check.Id,
                        Name = header.Key,
                        Value = string.Join(", ", header.Value)
                    });
                }
            }


            if ((int)response.StatusCode == monitor.ExpectedStatusCode)
            {
                if (monitor.MaxResponseTimeMs > 0 && check.ResponseTimeMs > monitor.MaxResponseTimeMs)
                {
                    check.IsSuccess = false;
                    check.CheckResultType = CheckResultType.ResponseTimeTooHigh;
                    check.ErrorMessage = $"Response time {check.ResponseTimeMs}ms exceeds limit {monitor.MaxResponseTimeMs}ms";
                }
                else
                {
                    check.IsSuccess = true;
                    check.CheckResultType = CheckResultType.Success;

                    if (monitor.IsHealthJson)
                    {
                        try
                        {
                            check.HealthJson = await response.Content.ReadAsStringAsync(ct);
                        }
                        catch
                        {
                            // ignore json read errors
                        }
                    }
                }
            }
            else
            {
                check.IsSuccess = false;
                check.CheckResultType = CheckResultType.UnexpectedStatusCode;
                check.ErrorMessage = $"Expected {monitor.ExpectedStatusCode}, got {(int)response.StatusCode}";
            }
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            check.ResponseTimeMs = sw.ElapsedMilliseconds;
            check.IsSuccess = false;
            check.CheckResultType = CheckResultType.Timeout;
            check.ErrorMessage = $"Request timed out after {monitor.TimeoutSeconds}s";
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            sw.Stop();
            check.ResponseTimeMs = sw.ElapsedMilliseconds;
            check.IsSuccess = false;
            check.CheckResultType = CheckResultType.ConnectionError;
            check.ErrorMessage = $"Connection error: {ex.Message}";
        }
        catch (Exception ex)
        {
            sw.Stop();
            check.ResponseTimeMs = sw.ElapsedMilliseconds;
            check.IsSuccess = false;

            if (ex.Message.Contains("DNS") || ex.Message.Contains("Name or service not known") || ex.Message.Contains("No such host"))
            {
                check.CheckResultType = CheckResultType.DnsError;
                check.ErrorMessage = $"DNS error: {ex.Message}";
            }
            else
            {
                check.CheckResultType = CheckResultType.ConnectionError;
                check.ErrorMessage = $"Error: {ex.Message}";
            }
        }

        await _checkRepo.CreateAsync(check);
        return check;
    }

    public async Task<SslCertificateCheck> CheckSslAsync(MonitorTarget monitor, CancellationToken ct)
    {
        var sslCheck = new SslCertificateCheck
        {
            MonitorId = monitor.Id,
            CheckedAt = DateTime.UtcNow
        };

        try
        {
            if (!Uri.TryCreate(monitor.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            {
                sslCheck.IsValid = false;
                sslCheck.ErrorMessage = "SSL check is only available for HTTPS URLs";
                await _sslRepo.CreateAsync(sslCheck);
                return sslCheck;
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
                {
                    if (cert == null) return false;
                    sslCheck.ExpiryDate = cert.NotAfter.ToUniversalTime();
                    sslCheck.DaysRemaining = (int)(cert.NotAfter.ToUniversalTime() - DateTime.UtcNow).TotalDays;
                    sslCheck.IsValid = cert.NotAfter > DateTime.Now;
                    sslCheck.CommonName = cert.GetNameInfo(X509NameType.SimpleName, false);
                    sslCheck.Issuer = cert.Issuer;
                    return true;
                }
            };

            using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(monitor.TimeoutSeconds > 0 ? monitor.TimeoutSeconds : 30) };
            using var request = new HttpRequestMessage(new HttpMethod(monitor.HttpMethod ?? "GET"), monitor.Url);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (Exception ex)
        {
            sslCheck.IsValid = false;
            sslCheck.ErrorMessage = $"SSL check failed: {ex.Message}";
        }

        await _sslRepo.CreateAsync(sslCheck);
        return sslCheck;
    }
}
