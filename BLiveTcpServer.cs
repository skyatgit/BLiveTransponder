using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace BLiveTransponder;

public class BLiveTcpServer
{
    private readonly ConcurrentDictionary<Guid, Socket> _clients = new();
    private readonly bool _enable;
    private readonly ushort _port;
    public string Info = "Tcp服务器未启动";

    public BLiveTcpServer()
    {
        (_enable, _port) = BLiveConfig.GetTcpServerConfig();
        if (_enable) StartAsync();
    }

    private async void StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        try
        {
            listener.Start();
        }
        catch (Exception e)
        {
            Info = $"Tcp服务器在 localhost:{_port} 上启动失败,原因:{e.Message}";
            return;
        }

        Info = $"Tcp服务器在 localhost:{_port} 上启动成功";
        while (_enable)
        {
            var clientSocket = await listener.AcceptTcpClientAsync();
            var clientId = Guid.NewGuid();
            _clients.TryAdd(clientId, clientSocket.Client);
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
            await clientSocket.SendAsync(rawData, SocketFlags.None);
        }
        catch (Exception)
        {
            clientSocket.Dispose();
            _clients.TryRemove(clientId, out _);
        }
    }
}