using System.Text.RegularExpressions;
using Godot;

namespace BLiveTransponder;

public partial class LoginPanel : Panel
{
    private double _coolDown;
    private string _qrcodeKey;
    private TextureButton _qrTextureButton;
    internal string Sessdata;

    public override void _Ready()
    {
        HideUi();
        _qrTextureButton = GetNode("QrTextureButton") as TextureButton;
    }

    private void SwitchUi()
    {
        if (Visible)
            HideUi();
        else
            ShowUi();
    }

    private void ShowUi()
    {
        if (Visible) return;
        Visible = true;
        _coolDown = 1;
    }

    internal void HideUi()
    {
        Visible = false;
        _qrcodeKey = null;
        _coolDown = 0;
    }

    private void CancelLogin()
    {
        Sessdata = null;
        HideUi();
    }

    private void RefreshQr()
    {
        _qrcodeKey = BLiveBase.GetQrCodeKey();
        var image = BLiveBase.GetQrImage(_qrcodeKey);
        _qrTextureButton.TextureNormal = image;
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        _coolDown += delta;
        if (_coolDown < 1) return;
        _coolDown = 0;
        var (code, url) = BLiveBase.GetQrResult(_qrcodeKey);
        switch (code)
        {
            case 0:
                HideUi();
                var match = Regex.Match(url, @"SESSDATA=(.+?)&");
                Sessdata = match.Groups[1].Value;
                return;
            case 86038:
                RefreshQr();
                break;
        }
    }
}