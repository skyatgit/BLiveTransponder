using Godot;

namespace BLiveTransponder;

public static class BLiveConfig
{
    private const string Path = "user://settings.cfg";
    private static readonly ConfigFile Config = new();

    public static ulong GetRoomId()
    {
        Config.Load(Path);
        return Config.GetValue("config", "roomId", 0).AsUInt64();
    }

    public static (string, string) GetCookie()
    {
        Config.Load(Path);
        return (Config.GetValue("Cookie", "sessdata", "").AsString(), Config.GetValue("Cookie", "refreshToken", "").AsString());
    }

    public static void SaveRoomId(ulong roomId)
    {
        Config.SetValue("config", "roomId", roomId);
        Config.Save(Path);
    }

    public static void SaveCookie(string sessdata, string refreshToken)
    {
        Config.SetValue("Cookie", "sessdata", sessdata);
        Config.SetValue("Cookie", "refreshToken", refreshToken);
        Config.Save(Path);
    }
}