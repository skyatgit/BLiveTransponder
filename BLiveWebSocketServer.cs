using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace BLiveTransponder;

public class BLiveWebSocketServer
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private readonly bool _enable;
    private readonly ushort _port;
    public string Info;

    public BLiveWebSocketServer()
    {
        (_enable, _port) = BLiveConfig.GetWebSocketServerConfig();
        if (_enable) StartAsync();
    }

    private async void StartAsync()
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{_port}/BLiveSMS/");
        try
        {
            listener.Start();
        }
        catch (Exception e)
        {
            Info = $"WebSocket服务器在 ws://localhost:{_port}/BLiveSMS/ 上启动失败,原因:{e.Message}";
            return;
        }

        Info = $"WebSocket服务器在 ws://localhost:{_port}/BLiveSMS/ 上启动成功";
        while (_enable)
        {
            var context = await listener.GetContextAsync();
            if (!context.Request.IsWebSocketRequest) continue;
            WebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            var clientId = Guid.NewGuid();
            var clientSocket = webSocketContext.WebSocket;
            _clients.TryAdd(clientId, clientSocket);
        }
    }

    public void SendMessage(string message)
    {
        if (!_enable) return;
        foreach (var clientId in _clients.Keys) SendToClient(clientId, message);
    }

    private async void SendToClient(Guid clientId, string message)
    {
        if (!_clients.TryGetValue(clientId, out var clientSocket)) return;
        try
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await clientSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception)
        {
            clientSocket.Dispose();
            _clients.TryRemove(clientId, out _);
        }
    }
}