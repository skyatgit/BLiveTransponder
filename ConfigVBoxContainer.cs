using System;
using Godot;

namespace BLiveTransponder;

public partial class ConfigVBoxContainer : VBoxContainer
{
    private CheckBox _enableTcpServer;
    private CheckBox _enableWebSocketServer;
    private Label _infoLabel;
    private LineEdit _tcpServerPort;
    private LineEdit _webSocketServerPort;

    public override void _Ready()
    {
        _enableWebSocketServer = GetNode("WebSocketServerHBoxContainer/CheckBox") as CheckBox;
        _enableTcpServer = GetNode("TcpServerHBoxContainer/CheckBox") as CheckBox;
        _webSocketServerPort = GetNode("WebSocketServerHBoxContainer/LineEdit") as LineEdit;
        _tcpServerPort = GetNode("TcpServerHBoxContainer/LineEdit") as LineEdit;
        _infoLabel = GetNode("Label") as Label;
        LoadConfig();
    }

    private void LoadConfig()
    {
        LoadWebSocketServerConfig();
        LoadTcpServerConfig();
    }

    private void SaveConfig()
    {
        var parseWebSocketServerPortSuccess = uint.TryParse(_webSocketServerPort.Text, out var webSocketServerPort);
        var parseTcpServerPortSuccess = uint.TryParse(_tcpServerPort.Text, out var tcpServerPort);
        var success = true;
        var reason = "";
        if (!parseWebSocketServerPortSuccess || webSocketServerPort > 65535)
        {
            success = false;
            reason += ",WebSocketServer端口无效";
        }

        if (!parseTcpServerPortSuccess || tcpServerPort > 65535)
        {
            success = false;
            reason += ",TcpServer端口无效";
        }

        if (webSocketServerPort == tcpServerPort)
        {
            success = false;
            reason += ",WebSocketServer端口不可以和TcpServer端口相同";
        }

        if (success)
        {
            SaveWebSocketServerConfig(webSocketServerPort);
            SaveTcpServerConfig(tcpServerPort);
        }

        var time = DateTime.Now;
        _infoLabel.Text = success ? $"修改成功,修改的内容将在下次启动时生效:{time}" : $"修改失败{reason}:{time}";
    }

    private void LoadWebSocketServerConfig()
    {
        var (enable, port) = BLiveConfig.GetWebSocketServerConfig();
        (_enableWebSocketServer.ButtonPressed, _webSocketServerPort.Text) = (enable, port.ToString());
    }

    private void SaveWebSocketServerConfig(uint port)
    {
        BLiveConfig.SaveWebSocketServerConfig(_enableWebSocketServer.ButtonPressed, port);
    }

    private void LoadTcpServerConfig()
    {
        var (enable, port) = BLiveConfig.GetTcpServerConfig();
        (_enableTcpServer.ButtonPressed, _tcpServerPort.Text) = (enable, port.ToString());
    }

    private void SaveTcpServerConfig(uint port)
    {
        BLiveConfig.SaveTcpServerConfig(_enableTcpServer.ButtonPressed, port);
    }
}