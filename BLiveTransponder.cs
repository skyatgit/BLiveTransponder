using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BLiveAPI;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using HttpClient = System.Net.Http.HttpClient;

namespace BLiveTransponder;

public partial class BLiveTransponder : Control
{
    private readonly BLiveApi _api = new();
    private RichTextLabel _label;
    private string _qrcodeKey;
    private TextureButton _qrTextureButton;
    private Panel _loginPanel;

    public override async void _Ready()
    {
        _label = GetNode("RichTextLabel") as RichTextLabel;
        _qrTextureButton = GetNode("LoginPanel/QrTextureButton") as TextureButton;
        _loginPanel = GetNode("LoginPanel") as Panel;
        _api.OpSendSmsReply += OpSendSmsReplyEvent;
        _api.DanmuMsg += DanmuMsgEvent;
        try
        {
            await _api.Connect(732, 3, await Login());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        await _api.Close();
    }

    private void DanmuMsgEvent(object sender, (string msg, ulong userId, string userName, int guardLevel, string face, JObject rawData) e)
    {
        _label.AddText($"{e.userName}:{e.msg}\n");
    }

    [TargetCmd("OTHERS")]
    private static void OpSendSmsReplyEvent(object sender, (string cmd, string hitCmd, JObject rawData) e)
    {
        Console.WriteLine($"{e.cmd}");
        
    }

    public override void _Process(double delta)
    {
    }

    private async Task<string> Login()
    {
        while (true)
        {
            var (code, url) = GetQrResult(_qrcodeKey);
            switch (code)
            {
                case 0:
                    _loginPanel.Visible = false;
                    var match = Regex.Match(url, @"SESSDATA=(.+?)&");
                    return match.Groups[1].Value;
                case 86038:
                    RefreshQr();
                    break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    private void RefreshQr()
    {
        _qrcodeKey = GetQrCodeKey();
        var image = GetQrImage(_qrcodeKey);
        _qrTextureButton.TextureNormal = image;
    }

    private static (int, string) GetQrResult(string qrcodeKey)
    {
        try
        {
            var resultUrl = $"https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qrcodeKey}";
            var result = new HttpClient().GetStringAsync(resultUrl).Result;
            var jsonResult = (JObject)JsonConvert.DeserializeObject(result);
            var code = (int)jsonResult?["data"]?["code"];
            var url = (string)jsonResult?["data"]?["url"];
            return (code, url);
        }
        catch (ArgumentException)
        {
            throw new DomainNameEncodingException();
        }
        catch
        {
            throw new NetworkException();
        }
    }

    private static ImageTexture GetQrImage(string qrcodeKey)
    {
        var url = $"https://passport.bilibili.com/h5-app/passport/login/scan?navhide=1&qrcode_key={qrcodeKey}";
        using var qrcodeData = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrcodeImageData = new PngByteQRCode(qrcodeData).GetGraphic(4);
        using var image = new Image();
        image.LoadPngFromBuffer(qrcodeImageData);
        return ImageTexture.CreateFromImage(image);
    }

    private static string GetQrCodeKey()
    {
        try
        {
            const string url = "https://passport.bilibili.com/x/passport-login/web/qrcode/generate";
            var result = new HttpClient().GetStringAsync(url).Result;
            var jsonResult = (JObject)JsonConvert.DeserializeObject(result);
            var qrcodeKey = (string)jsonResult?["data"]?["qrcode_key"];
            return qrcodeKey;
        }
        catch (ArgumentException)
        {
            throw new DomainNameEncodingException();
        }
        catch
        {
            throw new NetworkException();
        }
    }
}