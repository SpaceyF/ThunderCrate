using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ThunderCrate;

public partial class App : Application
{
    private NotifyIcon _tray = null!;
    private Config _cfg = null!;
    private InstallServer _server = null!;
    private StatusWindow? _status;
    private readonly List<string> _log = new();

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunName = "ThunderCrate";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _cfg = Config.Load();

        _server = new InstallServer(_cfg);
        _server.Log += AddLog;
        _server.Installed += OnInstalled;

        try { _server.Start(); }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open port {_cfg.Port}.\n{ex.Message}\n\nAnother app may be using it.",
                "ThunderCrate", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        BuildTray();
        AddLog($"ThunderCrate ready. Mods: {_cfg.ModsPath}");
    }

    private void BuildTray()
    {
        _tray = new NotifyIcon
        {
            Icon = LoadIcon(),
            Visible = true,
            Text = "ThunderCrate"
        };
        _tray.DoubleClick += (_, _) => ShowStatus();

        RebuildMenu();
    }

    private void RebuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add($"Port {_cfg.Port}  ({(_server.Running ? "listening" : "stopped")})", null,
            (_, _) => ShowStatus());
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Open Mods folder", null, (_, _) => OpenMods());
        menu.Items.Add("Set Mods folder…", null, (_, _) => SetModsFolder());

        var deps = new ToolStripMenuItem("Install dependencies") { Checked = _cfg.InstallDependencies };
        deps.Click += (_, _) => { _cfg.InstallDependencies = !_cfg.InstallDependencies; _cfg.Save(); RebuildMenu(); };
        menu.Items.Add(deps);

        var startup = new ToolStripMenuItem("Run at startup") { Checked = IsStartupEnabled() };
        startup.Click += (_, _) => { SetStartup(!IsStartupEnabled()); RebuildMenu(); };
        menu.Items.Add(startup);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Status / log…", null, (_, _) => ShowStatus());
        menu.Items.Add("Quit", null, (_, _) => Quit());

        _tray.ContextMenuStrip = menu;
    }

    private void OnInstalled(string fullName, bool ok)
    {
        Dispatcher.Invoke(() =>
        {
            _tray.ShowBalloonTip(3000, "ThunderCrate",
                ok ? $"Installed {fullName}" : $"Failed: {fullName}",
                ok ? ToolTipIcon.Info : ToolTipIcon.Error);
            _status?.Refresh();
        });
    }

    private void AddLog(string msg)
    {
        string line = $"{DateTime.Now:HH:mm:ss}  {msg}";
        lock (_log)
        {
            _log.Add(line);
            if (_log.Count > 400) _log.RemoveAt(0);
        }
        Dispatcher.BeginInvoke(() => _status?.AppendLog(line));
    }

    public string[] LogSnapshot() { lock (_log) return _log.ToArray(); }
    public Config Cfg => _cfg;
    public bool ServerRunning => _server.Running;

    private void ShowStatus()
    {
        if (_status == null)
        {
            _status = new StatusWindow(this);
            _status.Closed += (_, _) => _status = null;
        }
        _status.Show();
        _status.Activate();
    }

    private void OpenMods()
    {
        try
        {
            Directory.CreateDirectory(_cfg.ModsPath);
            Process.Start(new ProcessStartInfo(_cfg.ModsPath) { UseShellExecute = true });
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    public void SetModsFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Pick your BONELAB Mods folder",
            SelectedPath = Directory.Exists(_cfg.ModsPath) ? _cfg.ModsPath : ""
        };
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _cfg.ModsPath = dlg.SelectedPath;
            _cfg.Save();
            AddLog($"Mods folder set to {_cfg.ModsPath}");
            _status?.Refresh();
        }
    }

    private bool IsStartupEnabled()
    {
        using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey);
        return k?.GetValue(RunName) != null;
    }

    private void SetStartup(bool on)
    {
        try
        {
            using var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKey);
            if (on)
            {
                string exe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                k?.SetValue(RunName, $"\"{exe}\"");
            }
            else k?.DeleteValue(RunName, false);
            _cfg.RunAtStartup = on;
            _cfg.Save();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private static System.Drawing.Icon LoadIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/icon.ico");
            var stream = Application.GetResourceStream(uri)?.Stream;
            if (stream != null) return new System.Drawing.Icon(stream);
        }
        catch { }
        return System.Drawing.SystemIcons.Application;
    }

    private void Quit()
    {
        _server.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        Shutdown();
    }
}
