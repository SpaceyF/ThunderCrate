using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace ThunderCrate;

public class InstallResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Version { get; set; } = "";
    public int FileCount { get; set; }
    public List<string> Installed { get; set; } = new();
}

public static class Installer
{
    private static readonly HttpClient Http = CreateClient();

    private static readonly string[] MetaFiles =
        { "manifest.json", "icon.png", "readme.md", "changelog.md", "license", "license.md", "license.txt" };

    private static readonly string[] GameFolders =
        { "mods", "userdata", "userlibs", "plugins", "melonloader" };

    private static HttpClient CreateClient()
    {
        var c = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
        c.DefaultRequestHeaders.UserAgent.ParseAdd("ThunderCrate/1.0");
        c.Timeout = TimeSpan.FromMinutes(5);
        return c;
    }

    public static async Task<InstallResult> InstallAsync(
        string ns, string name, string? version, Config cfg, Action<string> log)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int total = 0;
        var installed = new List<string>();

        try
        {
            await InstallOneAsync(ns, name, version, cfg, log, visited, installed, n => total += n, 0);
        }
        catch (Exception ex)
        {
            return new InstallResult { Ok = false, Message = ex.Message };
        }

        if (installed.Count == 0)
            return new InstallResult { Ok = false, Message = "Nothing was installed (no files matched)." };

        return new InstallResult
        {
            Ok = true,
            Message = $"Installed {installed.Count} package(s), {total} file(s).",
            FullName = $"{ns}-{name}",
            Version = version ?? "",
            FileCount = total,
            Installed = installed
        };
    }

    private static async Task InstallOneAsync(
        string ns, string name, string? version, Config cfg, Action<string> log,
        HashSet<string> visited, List<string> installed, Action<int> addCount, int depth)
    {
        string key = $"{ns}-{name}";
        if (!visited.Add(key)) return;
        if (name.Equals("MelonLoader", StringComparison.OrdinalIgnoreCase))
        {
            log($"skip {key} (MelonLoader is installed separately)");
            return;
        }

        log($"{(depth > 0 ? "  dep: " : "")}fetching {ns}/{name}...");

        var meta = await FetchMetaAsync(ns, name, version);
        if (meta == null)
            throw new Exception($"Could not find package {ns}/{name} on Thunderstore.");

        string ver = meta.Value.version;
        string url = meta.Value.download;
        var deps = meta.Value.dependencies;

        log($"{(depth > 0 ? "  " : "")}downloading {ns}-{name} {ver}...");
        byte[] zipBytes = await Http.GetByteArrayAsync(url);

        int count = ExtractPackage(zipBytes, cfg, log, depth > 0);
        addCount(count);
        installed.Add($"{ns}-{name} {ver}");
        log($"{(depth > 0 ? "  " : "")}-> {count} file(s) into place");

        if (cfg.InstallDependencies && deps != null)
        {
            foreach (var dep in deps)
            {
                var parts = dep.Split('-');
                if (parts.Length < 3) continue;
                string dns = parts[0];
                string dname = string.Join('-', parts[1..^1]);
                string dver = parts[^1];
                await InstallOneAsync(dns, dname, dver, cfg, log, visited, installed, addCount, depth + 1);
            }
        }
    }

    private static async Task<(string version, string download, List<string> dependencies)?> FetchMetaAsync(
        string ns, string name, string? version)
    {
        string api = version == null
            ? $"https://thunderstore.io/api/experimental/package/{ns}/{name}/"
            : $"https://thunderstore.io/api/experimental/package/{ns}/{name}/{version}/";

        try
        {
            using var resp = await Http.GetAsync(api);
            if (!resp.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var root = doc.RootElement;

            JsonElement v = version == null && root.TryGetProperty("latest", out var latest) ? latest : root;

            string ver = v.TryGetProperty("version_number", out var vn) ? vn.GetString() ?? "" : version ?? "";
            string dl = v.TryGetProperty("download_url", out var du)
                ? du.GetString() ?? ""
                : $"https://thunderstore.io/package/download/{ns}/{name}/{ver}/";

            var deps = new List<string>();
            if (v.TryGetProperty("dependencies", out var da) && da.ValueKind == JsonValueKind.Array)
                foreach (var d in da.EnumerateArray())
                    if (d.GetString() is string s) deps.Add(s);

            return (ver, dl, deps);
        }
        catch
        {
            return null;
        }
    }

    private static int ExtractPackage(byte[] zipBytes, Config cfg, Action<string> log, bool isDep)
    {
        string modsPath = cfg.ModsPath;
        string gameRoot = Directory.GetParent(modsPath.TrimEnd('\\', '/'))?.FullName ?? modsPath;
        Directory.CreateDirectory(modsPath);

        using var ms = new MemoryStream(zipBytes);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

        // zip mirrors game folders vs loose dll
        bool gameRooted = zip.Entries.Any(e =>
        {
            var seg = FirstSegment(e.FullName);
            return seg != null && GameFolders.Contains(seg.ToLowerInvariant());
        });

        int copied = 0;
        foreach (var entry in zip.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;
            string full = entry.FullName.Replace('\\', '/');
            var segs = full.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segs.Length == 0) continue;

            string dest;
            if (gameRooted && GameFolders.Contains(segs[0].ToLowerInvariant()))
            {
                // map folder into the game root
                string top = segs[0].ToLowerInvariant();
                if (top == "melonloader") continue; // never touch MelonLoader
                string root = top == "mods" ? modsPath : Path.Combine(gameRoot, segs[0]);
                dest = Path.Combine(root, Path.Combine(segs[1..]));
            }
            else
            {
                if (segs.Length == 1 && MetaFiles.Contains(segs[0].ToLowerInvariant())) continue;
                string ext = Path.GetExtension(entry.Name).ToLowerInvariant();
                if (ext != ".dll") continue;
                dest = Path.Combine(modsPath, entry.Name);
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                entry.ExtractToFile(dest, overwrite: true);
                copied++;
            }
            catch (Exception ex)
            {
                log($"  ! could not write {entry.Name}: {ex.Message}");
            }
        }

        return copied;
    }

    private static string? FirstSegment(string path)
    {
        var segs = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segs.Length > 0 ? segs[0] : null;
    }
}
