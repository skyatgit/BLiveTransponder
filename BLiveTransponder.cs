using System;
using System.Threading;
using System.Web;
using BLiveAPI;
using Godot;
using Newtonsoft.Json.Linq;
using HttpClient = System.Net.Http.HttpClient;

namespace BLiveTransponder;

public partial class BLiveTransponder : Control
{
    private readonly BLiveApi _api = new();
    private readonly BLiveTcpServer _bLiveTcpServer = new();
    private readonly BLiveWebSocketServer _bLiveWebSocketServer = new();
    private readonly SemaphoreSlim _semaphore = new(1);
    private CheckButton _connectCheckButton;
    private RichTextLabel _dmRichTextLabel;
    private LoginPanel _loginPanel;
    private AudioStreamPlayer _player;
    private LineEdit _roomIdLineEdit;
    private Label _tcpClientCountLabel;
    private LinkButton _userLinkButton;
    private Label _webSocketClientCountLabel;

    public override void _Ready()
    {
        _dmRichTextLabel = GetNode("TabContainer/弹幕/DmRichTextLabel") as RichTextLabel;
        _userLinkButton = GetNode("TopColorRect/UserLinkButton") as LinkButton;
        _loginPanel = GetNode("LoginPanel") as LoginPanel;
        _connectCheckButton = GetNode("TopColorRect/ConnectCheckButton") as CheckButton;
        _roomIdLineEdit = GetNode("TopColorRect/RoomIdLineEdit") as LineEdit;
        _tcpClientCountLabel = GetNode("TopColorRect/TcpClientCountLabel") as Label;
        _webSocketClientCountLabel = GetNode("TopColorRect/WebSocketClientCountLabel") as Label;
        _player = GetNode("AudioStreamPlayer") as AudioStreamPlayer;
        _api.OpSendSmsReply += OpSendSmsReplyEvent;
        _api.OpAuthReply += OpAuthReplyEvent;
        _api.DanmuMsg += DanmuMsgEvent;
        _api.InteractWord += InteractWordEvent;
        _bLiveTcpServer.ServerStateChange += ServerStateChange;
        _bLiveTcpServer.ClientCountChange += TcpClientCountChange;
        _bLiveWebSocketServer.ServerStateChange += ServerStateChange;
        _bLiveWebSocketServer.ClientCountChange += WebSocketClientCountChange;
        var roomId = BLiveConfig.GetRoomConfig();
        if (roomId != 0) _roomIdLineEdit!.Text = roomId.ToString();
        _bLiveTcpServer.StartAsync();
        _bLiveWebSocketServer.StartAsync();
    }

    private void ServerStateChange(string result)
    {
        _dmRichTextLabel.AddText($"{result}\n");
    }

    private void TcpClientCountChange(string result)
    {
        _tcpClientCountLabel.Text = $"Tcp客户端:{result}";
    }

    private void WebSocketClientCountChange(string result)
    {
        _webSocketClientCountLabel.Text = $"WebSocket客户端:{result}";
    }

    private void OpAuthReplyEvent(object sender, (JObject authReply, ulong? roomId, byte[] rawData) e)
    {
        _connectCheckButton.Text = "已连接";
        _connectCheckButton.Disabled = false;
        _dmRichTextLabel.Clear();
        _dmRichTextLabel.AddText($"已成功连接至房间:{e.roomId}\n");
        ReadText($"已成功连接至房间:{e.roomId}");
    }

    private async void Toggled(bool connect)
    {
        if (connect)
        {
            SetConnectStatus(true);
            var success = ulong.TryParse(_roomIdLineEdit.Text, out var roomId);
            if (success)
            {
                try
                {
                    BLiveConfig.SaveRoomConfig(roomId);
                    await _api.Connect(roomId, 3, _loginPanel.Sessdata);
                }
                catch (Exception e)
                {
                    _dmRichTextLabel.AddText($"{e.Message}\n");
                    SetConnectStatus(false);
                }
            }
            else
            {
                _dmRichTextLabel.AddText("无效的房间号\n");
                SetConnectStatus(false);
            }
        }
        else
        {
            await _api.Close();
        }
    }

    private void SetConnectStatus(bool status)
    {
        if (status)
        {
            _roomIdLineEdit.Editable = false;
            _connectCheckButton.Disabled = true;
            _userLinkButton.Disabled = true;
            _loginPanel.HideUi();
            _connectCheckButton.Text = "连接中";
        }
        else
        {
            _roomIdLineEdit.Editable = true;
            _connectCheckButton.Disabled = false;
            _userLinkButton.Disabled = false;
            _connectCheckButton.Text = "未连接";
            _connectCheckButton.SetPressedNoSignal(false);
        }
    }

    private void DanmuMsgEvent(object sender, (string msg, ulong userId, string userName, int guardLevel, string face, JObject jsonRawData, byte[] rawData) e)
    {
        _dmRichTextLabel.AddText($"{e.userName}:{e.msg}\n");
        ReadText($"{e.userName}说{e.msg}");
    }

    private void InteractWordEvent(object sender, (int privilegeType, ulong userId, string userName, JObject jsonRawData, byte[] rawData) e)
    {
        _dmRichTextLabel.AddText($"{e.userName} 进入直播间\n");
        ReadText($"欢迎{e.userName}进入直播间");
    }

    [TargetCmd("ALL")]
    private void OpSendSmsReplyEvent(object sender, (string cmd, string hitCmd, JObject jsonRawData, byte[] rawData) e)
    {
        var data = BLiveBase.CreateSmsPacket(0, e.rawData);
        _bLiveWebSocketServer.SendSmsToClients(data);
        _bLiveTcpServer.SendSmsToClients(data);
    }

    private async void ReadText(string text)
    {
        await _semaphore.WaitAsync();
        var url = $"https://dict.youdao.com/dictvoice?audio={HttpUtility.UrlEncode(text)}&le=zh";
        var result = await new HttpClient().GetByteArrayAsync(url);
        (_player.Stream as AudioStreamMP3)!.Data = result;
        _player.Play();
        await ToSignal(_player, "finished");
        _semaphore.Release();
    }

    public override void _Process(double delta)
    {
    }
}