using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace ThunderCrate;

public partial class StatusWindow : Window
{
    private readonly App _app;

    public StatusWindow(App app)
    {
        InitializeComponent();
        _app = app;
        Refresh();
        foreach (var line in _app.LogSnapshot()) AppendLog(line);
    }

    public void Refresh()
    {
        var cfg = _app.Cfg;
        ModsPathText.Text = cfg.ModsPath;
        StatusLine.Text = _app.ServerRunning
            ? $"listening on http://127.0.0.1:{cfg.Port}"
            : "server stopped";
        StatusLine.Foreground = new SolidColorBrush(
            _app.ServerRunning ? Color.FromRgb(0x6b, 0xd6, 0xa8) : Color.FromRgb(0xd6, 0x6b, 0x6b));

        RecentList.Items.Clear();
        foreach (var r in cfg.Recent)
        {
            RecentList.Items.Add(new TextBlock
            {
                Text = $"• {r.FullName}  {r.Version}   ({r.When})",
                Foreground = new SolidColorBrush(Color.FromRgb(0xd0, 0xd4, 0xde)),
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 2)
            });
        }
        RecentEmpty.Visibility = cfg.Recent.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public void AppendLog(string line)
    {
        LogText.Text += (LogText.Text.Length == 0 ? "" : "\n") + line;
        LogScroll.ScrollToEnd();
    }

    private void OnSetFolder(object sender, RoutedEventArgs e) => _app.SetModsFolder();

    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            System.IO.Directory.CreateDirectory(_app.Cfg.ModsPath);
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(_app.Cfg.ModsPath) { UseShellExecute = true });
        }
        catch { }
    }
}
