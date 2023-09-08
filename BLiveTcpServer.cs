using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BLiveTransponder;

public class BLiveTcpServer
{
    private readonly ConcurrentDictionary<Guid, Socket> _clients = new();
    private readonly bool _enable;
    private readonly uint _port;

    public BLiveTcpServer()
    {
        (_enable, _port) = BLiveConfig.GetTcpServerConfig();
        if (_enable) StartAsync();
    }

    private async void StartAsync()
    {
        var listener = new TcpListener(IPAddress.Any, (int)_port);
        listener.Start();
        while (_enable)
        {
            var clientSocket = await listener.AcceptTcpClientAsync();
            var clientId = Guid.NewGuid();
            _clients.TryAdd(clientId, clientSocket.Client);
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
            await clientSocket.SendAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
        }
        catch (Exception)
        {
            clientSocket.Dispose();
            _clients.TryRemove(clientId, out _);
        }
    }
}