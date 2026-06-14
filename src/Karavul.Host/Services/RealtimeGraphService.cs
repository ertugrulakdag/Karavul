using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Karavul.Host.Services;

public class RealtimeGraphService
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();
    private readonly ILogger<RealtimeGraphService> _logger;

    public RealtimeGraphService(ILogger<RealtimeGraphService> logger)
    {
        _logger = logger;
    }

    public async Task HandleConnectionAsync(WebSocket webSocket)
    {
        var id = Guid.NewGuid();
        _sockets.TryAdd(id, webSocket);
        
        try
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
            }
        }
        catch (WebSocketException)
        {
            // Expected on client disconnect
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket connection error");
        }
        finally
        {
            _sockets.TryRemove(id, out _);
        }
    }

    public async Task BroadcastCheckResultAsync(bool isSuccess, string monitorName)
    {
        if (_sockets.IsEmpty) return;

        var message = JsonSerializer.Serialize(new
        {
            type = "check_result",
            isSuccess = isSuccess,
            monitorName = monitorName,
            timestamp = DateTime.UtcNow.ToString("o")
        });

        var bytes = Encoding.UTF8.GetBytes(message);
        var arraySegment = new ArraySegment<byte>(bytes);

        var tasks = _sockets.Values
            .Where(ws => ws.State == WebSocketState.Open)
            .Select(ws => ws.SendAsync(arraySegment, WebSocketMessageType.Text, true, CancellationToken.None));

        await Task.WhenAll(tasks);
    }
}
