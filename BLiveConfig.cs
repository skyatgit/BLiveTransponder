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

    public static void SaveRoomId(ulong roomId)
    {
        Config.SetValue("config", "roomId", roomId);
        Config.Save(Path);
    }
}