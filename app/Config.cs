using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThunderCrate;

public class Config
{
    public string ModsPath { get; set; } = DefaultModsPath();
    public int Port { get; set; } = 48752;
    public bool InstallDependencies { get; set; } = true;
    public bool RunAtStartup { get; set; } = false;
    public List<InstallRecord> Recent { get; set; } = new();

    [JsonIgnore]
    public static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ThunderCrate");

    [JsonIgnore]
    public static string FilePath => Path.Combine(Dir, "config.json");

    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string DefaultModsPath()
    {
        string[] guesses =
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\BONELAB\Mods",
            @"C:\Program Files\Steam\steamapps\common\BONELAB\Mods",
        };
        foreach (var g in guesses)
            if (Directory.Exists(g)) return g;
        return guesses[0];
    }

    public static Config Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var cfg = JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath), Opts);
                if (cfg != null)
                {
                    if (string.IsNullOrWhiteSpace(cfg.ModsPath)) cfg.ModsPath = DefaultModsPath();
                    if (cfg.Port <= 0) cfg.Port = 48752;
                    return cfg;
                }
            }
        }
        catch { }
        return new Config();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Opts));
        }
        catch { }
    }

    public void AddRecent(InstallRecord r)
    {
        Recent.RemoveAll(x => x.FullName == r.FullName);
        Recent.Insert(0, r);
        if (Recent.Count > 25) Recent.RemoveRange(25, Recent.Count - 25);
        Save();
    }
}

public class InstallRecord
{
    public string FullName { get; set; } = "";
    public string Version { get; set; } = "";
    public string When { get; set; } = "";
    public int FileCount { get; set; }
}
