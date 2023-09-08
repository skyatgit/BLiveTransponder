using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BLiveAPI;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using HttpClient = System.Net.Http.HttpClient;

namespace BLiveTransponder;

public static class BLiveBase
{
    public static (int, string, string) GetQrResult(string qrcodeKey)
    {
        try
        {
            var resultUrl = $"https://passport.bilibili.com/x/passport-login/web/qrcode/poll?qrcode_key={qrcodeKey}";
            var result = new HttpClient().GetStringAsync(resultUrl).Result;
            var jsonResult = (JObject)JsonConvert.DeserializeObject(result);
            var code = (int)jsonResult?["data"]?["code"];
            var url = (string)jsonResult?["data"]?["url"];
            var refreshToken = (string)jsonResult?["data"]?["refresh_token"];
            return (code, url, refreshToken);
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

    public static bool RefreshCookie(ref string sessdata, ref string refreshToken, ref string csrf)
    {
        const string cookieInfoUrl = "https://passport.bilibili.com/x/passport-login/web/cookie/info";
        try
        {
            var client = new HttpClient(new HttpClientHandler { UseCookies = false });
            client.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={sessdata}");
            var cookieInfo = client.GetStringAsync(cookieInfoUrl).Result;
            var cookieInfoJsonResult = (JObject)JsonConvert.DeserializeObject(cookieInfo);
            var refresh = (bool?)cookieInfoJsonResult?.SelectToken("data.refresh");
            switch (refresh)
            {
                case null:
                    Console.WriteLine("cookie失效");
                    (sessdata, refreshToken, csrf) = (null, null, null);
                    break;
                case true:
                    Console.WriteLine("需要刷新");
                    (sessdata, refreshToken, csrf) = RenewCookie(sessdata, refreshToken, csrf);
                    break;
                case false:
                    Console.WriteLine("不需要刷新");
                    break;
            }

            return refresh is null;
        }
        catch (ArgumentException)
        {
            throw new DomainNameEncodingException();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new NetworkException();
        }
    }

    private static string GetCorrespondPath()
    {
        const string publicKey = @"-----BEGIN PUBLIC KEY-----
            MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQDLgd2OAkcGVtoE3ThUREbio0Eg
            Uc/prcajMKXvkCKFCWhJYJcLkcM2DKKcSeFpD/j6Boy538YXnR6VhcuUJOhH2x71
            nzPjfdTcqMz7djHum0qSZA0AyCBDABUqCrfNgCiJ00Ra7GmRj+YCK1NJEuewlb40
            JNrRuoEUXpabUzGB8QIDAQAB -----END PUBLIC KEY-----";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKey);
        var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes($"refresh_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"), RSAEncryptionPadding.OaepSHA256);
        return Convert.ToHexString(encrypted);
    }

    private static string GetRefreshCsrf(string sessdata)
    {
        var refreshCsrfUrl = $"https://www.bilibili.com/correspond/1/{GetCorrespondPath()}";
        try
        {
            var client = new HttpClient(new HttpClientHandler { UseCookies = false, AutomaticDecompression = DecompressionMethods.GZip });
            client.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={sessdata}");
            var htmlDoc = client.GetStringAsync(refreshCsrfUrl).Result;
            return Regex.Match(htmlDoc, @"<div id=""1-name"">(.+?)</div>").Groups[1].Value;
        }
        catch (ArgumentException)
        {
            throw new DomainNameEncodingException();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new NetworkException();
        }
    }

    private static (string, string, string) RenewCookie(string sessdata, string refreshToken, string csrf)
    {
        var refreshCsrf = GetRefreshCsrf(sessdata);
        const string refreshUrl = "https://passport.bilibili.com/x/passport-login/web/cookie/refresh";
        try
        {
            var client = new HttpClient(new HttpClientHandler { UseCookies = false });
            client.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={sessdata}");
            var data = new Dictionary<string, string> { { "csrf", csrf }, { "refresh_csrf", refreshCsrf }, { "source", "main_web" }, { "refresh_token", refreshToken } };
            var response = client.PostAsync(refreshUrl, new FormUrlEncodedContent(data)).Result;
            var refreshJsonResult = (JObject)JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
            var newRefreshToken = (string)refreshJsonResult?.SelectToken("data.refresh_token");
            if (newRefreshToken is null) return (sessdata, refreshToken, csrf);
            var headers = response.Headers.ToString();
            var newSessdata = Regex.Match(headers, @"SESSDATA=(.+?);").Groups[1].Value;
            var newCsrf = Regex.Match(headers, @"bili_jct=(.+?);").Groups[1].Value;
            ConfirmRefresh(newSessdata, refreshToken, newCsrf);
            return (newSessdata, newRefreshToken, newCsrf);
        }
        catch (ArgumentException)
        {
            throw new DomainNameEncodingException();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new NetworkException();
        }
    }

    private static void ConfirmRefresh(string sessdata, string refreshToken, string csrf)
    {
        const string confirmUrl = "https://passport.bilibili.com/x/passport-login/web/confirm/refresh";
        try
        {
            var client = new HttpClient(new HttpClientHandler { UseCookies = false });
            client.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={sessdata}");
            var data = new Dictionary<string, string> { { "csrf", csrf }, { "refresh_token", refreshToken } };
            client.PostAsync(confirmUrl, new FormUrlEncodedContent(data)).Wait();
        }
        catch (ArgumentException)
        {
            throw new DomainNameEncodingException();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new NetworkException();
        }
    }
}