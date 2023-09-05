using System;
using System.Net.Http;
using BLiveAPI;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using HttpClient = System.Net.Http.HttpClient;

namespace BLiveTransponder;

public static class BLiveBase
{
    public static (int, string) GetQrResult(string qrcodeKey)
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

    public static ImageTexture GetQrImage(string qrcodeKey)
    {
        var url = $"https://passport.bilibili.com/h5-app/passport/login/scan?navhide=1&qrcode_key={qrcodeKey}";
        using var qrcodeData = QRCodeGenerator.GenerateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrcodeImageData = new PngByteQRCode(qrcodeData).GetGraphic(4);
        using var image = new Image();
        image.LoadPngFromBuffer(qrcodeImageData);
        return ImageTexture.CreateFromImage(image);
    }

    public static string GetQrCodeKey()
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

    public static string GetMyInfo(string sessdata)
    {
        try
        {
            var client = new HttpClient(new HttpClientHandler { UseCookies = false });
            client.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={sessdata}");
            var userInfoResult = client.GetStringAsync("https://api.bilibili.com/x/space/v2/myinfo").Result;
            var userInfoJsonResult = (JObject)JsonConvert.DeserializeObject(userInfoResult);
            return (string)userInfoJsonResult?["data"]?["profile"]?["name"];
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