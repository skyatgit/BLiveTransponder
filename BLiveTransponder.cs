using System;
using BLiveAPI;
using Godot;
using Newtonsoft.Json.Linq;

namespace BLiveTransponder;

public partial class BLiveTransponder : Control
{
    private readonly BLiveApi _api = new();
    private readonly BLiveWebSocketServer _bLiveWebSocketServer = new();
    private CheckButton _connectCheckButton;
    private RichTextLabel _label;
    private LoginPanel _loginPanel;
    private LineEdit _roomIdLineEdit;
    private TextureButton _userTextureButton;

    public override void _Ready()
    {
        _label = GetNode("RichTextLabel") as RichTextLabel;
        _userTextureButton = GetNode("TopColorRect/UserTextureButton") as TextureButton;
        _loginPanel = GetNode("LoginPanel") as LoginPanel;
        _connectCheckButton = GetNode("TopColorRect/ConnectCheckButton") as CheckButton;
        _roomIdLineEdit = GetNode("TopColorRect/RoomIdLineEdit") as LineEdit;
        _api.OpSendSmsReply += OpSendSmsReplyEvent;
        _api.OpAuthReply += OpAuthReplyEvent;
        _api.DanmuMsg += DanmuMsgEvent;
        var roomId = BLiveConfig.GetRoomId();
        if (roomId != 0) _roomIdLineEdit!.Text = roomId.ToString();
    }

    private void OpAuthReplyEvent(object sender, (JObject authReply, ulong? roomId, byte[] rawData) e)
    {
        _connectCheckButton.Text = "已连接";
        _connectCheckButton.Disabled = false;
        _label.Clear();
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
                    BLiveConfig.SaveRoomId(roomId);
                    await _api.Connect(roomId, 3, _loginPanel.Sessdata);
                }
                catch (Exception e)
                {
                    _label.AddText($"{e.Message}\n");
                    SetConnectStatus(false);
                }
            }
            else
            {
                _label.AddText("无效的房间号\n");
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
            _userTextureButton.Disabled = true;
            _loginPanel.HideUi();
            _connectCheckButton.Text = "连接中";
        }
        else
        {
            _roomIdLineEdit.Editable = true;
            _connectCheckButton.Disabled = false;
            _userTextureButton.Disabled = false;
            _connectCheckButton.Text = "未连接";
            _connectCheckButton.SetPressedNoSignal(false);
        }
    }

    private void DanmuMsgEvent(object sender, (string msg, ulong userId, string userName, int guardLevel, string face, JObject rawData) e)
    {
        _label.AddText($"{e.userName}:{e.msg}\n");
    }

    [TargetCmd("ALL")]
    private void OpSendSmsReplyEvent(object sender, (string cmd, string hitCmd, JObject rawData) e)
    {
        _bLiveWebSocketServer.SendMessage(e.rawData.ToString());
    }

    public override void _Process(double delta)
    {
    }
}