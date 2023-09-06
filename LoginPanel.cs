using System.Text.RegularExpressions;
using Godot;

namespace BLiveTransponder;

public partial class LoginPanel : Panel
{
    private double _coolDown;
    private string _qrcodeKey;
    private TextureButton _qrTextureButton;
    private LinkButton _userLinkButton;
    internal string Sessdata;

    public override void _Ready()
    {
        HideUi();
        _qrTextureButton = GetNode("QrTextureButton") as TextureButton;
        _userLinkButton = GetNode("../TopColorRect/UserLinkButton") as LinkButton;
        LoadCookie();
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
    }

    private void CancelLogin()
    {
        SetCookie(null, null);
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
        var (code, url, refreshToken) = BLiveBase.GetQrResult(_qrcodeKey);
        switch (code)
        {
            case 0:
                HideUi();
                var match = Regex.Match(url, @"SESSDATA=(.+?)&");
                SetCookie(match.Groups[1].Value, refreshToken);
                return;
            case 86038:
                RefreshQr();
                break;
        }
    }

    private void SetCookie(string sessdata, string refreshToken)
    {
        //TODO 通过refreshToken刷新Cookie
        var userName = BLiveBase.GetMyInfo(sessdata);
        Sessdata = userName is null ? null : sessdata;
        _userLinkButton.Text = userName is null ? "未登录:游客" : $"已登录:{userName}";
        BLiveConfig.SaveCookie(userName is null ? null : sessdata, userName is null ? null : refreshToken);
    }

    private void LoadCookie()
    {
        var (sessdata, refreshToken) = BLiveConfig.GetCookie();
        SetCookie(sessdata, refreshToken);
    }
}