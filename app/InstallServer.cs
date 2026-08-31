using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ThunderCrate;

public class InstallServer
{
    private readonly Config _cfg;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    public event Action<string>? Log;
    public event Action<string, bool>? Installed; // fullName, ok

    public InstallServer(Config cfg) => _cfg = cfg;

    public bool Running => _listener?.IsListening == true;

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        // localhost only, no admin/urlacl needed
        _listener.Prefixes.Add($"http://127.0.0.1:{_cfg.Port}/");
        _listener.Prefixes.Add($"http://localhost:{_cfg.Port}/");
        _listener.Start();
        _ = Task.Run(() => Loop(_cts.Token));
        Log?.Invoke($"listening on http://127.0.0.1:{_cfg.Port}");
    }

    public void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
        _listener = null;
    }

    private async Task Loop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch { break; }
            _ = Task.Run(() => Handle(ctx));
        }
    }

    private async Task Handle(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;
        // let the thunderstore page call us
        res.AddHeader("Access-Control-Allow-Origin", "*");
        res.AddHeader("Access-Control-Allow-Headers", "Content-Type");
        res.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");

        try
        {
            if (req.HttpMethod == "OPTIONS") { res.StatusCode = 204; res.Close(); return; }

            string path = req.Url?.AbsolutePath.TrimEnd('/').ToLowerInvariant() ?? "";

            if (path == "/ping")
            {
                WriteJson(res, new { app = "ThunderCrate", version = "1.0.0", mods = _cfg.ModsPath });
                return;
            }

            if (path == "/install" && req.HttpMethod == "POST")
            {
                await HandleInstall(req, res);
                return;
            }

            res.StatusCode = 404;
            WriteJson(res, new { ok = false, message = "unknown endpoint" });
        }
        catch (Exception ex)
        {
            try { res.StatusCode = 500; WriteJson(res, new { ok = false, message = ex.Message }); } catch { }
        }
    }

    private async Task HandleInstall(HttpListenerRequest req, HttpListenerResponse res)
    {
        string body;
        using (var r = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8))
            body = await r.ReadToEndAsync();

        string ns = "", name = "", version = "";
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            ns = Get(root, "namespace");
            name = Get(root, "name");
            version = Get(root, "version");
        }
        catch { }

        if (string.IsNullOrWhiteSpace(ns) || string.IsNullOrWhiteSpace(name))
        {
            res.StatusCode = 400;
            WriteJson(res, new { ok = false, message = "missing namespace/name" });
            return;
        }

        Log?.Invoke($"install request: {ns}/{name} {version}");
        var result = await Installer.InstallAsync(
            ns, name, string.IsNullOrWhiteSpace(version) ? null : version, _cfg,
            m => Log?.Invoke(m));

        if (result.Ok)
        {
            _cfg.AddRecent(new InstallRecord
            {
                FullName = result.FullName,
                Version = result.Version,
                When = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                FileCount = result.FileCount
            });
        }
        Installed?.Invoke($"{ns}-{name}", result.Ok);
        WriteJson(res, new { ok = result.Ok, message = result.Message, installed = result.Installed });
    }

    private static string Get(JsonElement e, string prop) =>
        e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static void WriteJson(HttpListenerResponse res, object obj)
    {
        byte[] buf = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
        res.ContentType = "application/json";
        res.ContentLength64 = buf.Length;
        res.OutputStream.Write(buf, 0, buf.Length);
        res.Close();
    }
}
