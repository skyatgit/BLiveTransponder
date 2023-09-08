using Godot;

namespace BLiveTransponder;

public static class BLiveConfig
{
    private const string Path = "user://settings.cfg";
    private static readonly ConfigFile Config = new();

    public static ulong GetRoomConfig()
    {
        Config.Load(Path);
        return Config.GetValue("RoomConfig", "roomId", 0).AsUInt64();
    }

    public static void SaveRoomConfig(ulong roomId)
    {
        Config.SetValue("RoomConfig", "roomId", roomId);
        Config.Save(Path);
    }

    public static (string, string, string) GetCookie()
    {
        Config.Load(Path);
        return (Config.GetValue("Cookie", "sessdata", "").AsString(),
            Config.GetValue("Cookie", "refreshToken", "").AsString(),
            Config.GetValue("Cookie", "csrf", "").AsString());
    }

    public static void SaveCookie(string sessdata, string refreshToken, string csrf)
    {
        Config.SetValue("Cookie", "sessdata", sessdata);
        Config.SetValue("Cookie", "refreshToken", refreshToken);
        Config.SetValue("Cookie", "csrf", csrf);
        Config.Save(Path);
    }

    public static (bool, ushort) GetWebSocketServerConfig()
    {
        Config.Load(Path);
        return (Config.GetValue("WebSocketServerConfig", "enable", true).AsBool(),
            Config.GetValue("WebSocketServerConfig", "port", 19980).AsUInt16());
    }

    public static void SaveWebSocketServerConfig(bool enable, ushort port)
    {
        Config.SetValue("WebSocketServerConfig", "enable", enable);
        Config.SetValue("WebSocketServerConfig", "port", port);
        Config.Save(Path);
    }

    public static (bool, ushort) GetTcpServerConfig()
    {
        Config.Load(Path);
        return (Config.GetValue("TcpServerConfig", "enable", true).AsBool(),
            Config.GetValue("TcpServerConfig", "port", 19981).AsUInt16());
    }

    public static void SaveTcpServerConfig(bool enable, ushort port)
    {
        Config.SetValue("TcpServerConfig", "enable", enable);
        Config.SetValue("TcpServerConfig", "port", port);
        Config.Save(Path);
    }
}