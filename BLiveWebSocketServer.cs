using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using Godot;

namespace BLiveTransponder;

public class BLiveWebSocketServer
{
    private readonly Label _clientCountLabel;
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private readonly bool _enable;
    private readonly ushort _port;
    public string Info = "WebSocket服务器未启动";

    public BLiveWebSocketServer(Label clientCountLabel)
    {
        _clientCountLabel = clientCountLabel;
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
            _clientCountLabel.Text = $"WebSocket客户端:{_clients.Count}";
        }
    }

    public void SendMessage(byte[] rawData)
    {
        if (!_enable) return;
        foreach (var clientId in _clients.Keys) SendToClient(clientId, rawData);
    }

    private async void SendToClient(Guid clientId, byte[] rawData)
    {
        if (!_clients.TryGetValue(clientId, out var clientSocket)) return;
        try
        {
            await clientSocket.SendAsync(rawData, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        catch (Exception)
        {
            clientSocket.Dispose();
            _clients.TryRemove(clientId, out _);
            _clientCountLabel.Text = $"WebSocket客户端:{_clients.Count}";
        }
    }
}