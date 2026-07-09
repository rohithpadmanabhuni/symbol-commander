using System.Drawing;
using System.Windows.Forms;

namespace SymbolCommander.App.Tray;

/// <summary>WinForms NotifyIcon wrapper with a runtime-drawn icon (no binary assets).</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _enabledItem;

    public event Action? SettingsRequested;
    public event Action? ExitRequested;
    public event Action<bool>? GesturesToggled;

    public TrayIcon()
    {
        _enabledItem = new ToolStripMenuItem("Gestures enabled") { Checked = true, CheckOnClick = true };
        _enabledItem.CheckedChanged += (_, _) => GesturesToggled?.Invoke(_enabledItem.Checked);

        var settings = new ToolStripMenuItem("Settings…");
        settings.Click += (_, _) => SettingsRequested?.Invoke();

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) => ExitRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
            { _enabledItem, new ToolStripSeparator(), settings, new ToolStripSeparator(), exit });

        _icon = new NotifyIcon
        {
            Icon = DrawIcon(),
            Text = "Symbol Commander",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => SettingsRequested?.Invoke();
    }

    public void SetGesturesEnabled(bool on) => _enabledItem.Checked = on;

    public void ShowNotification(string title, string message, bool warning = false) =>
        _icon.ShowBalloonTip(3000, title, message, warning ? ToolTipIcon.Warning : ToolTipIcon.Info);

    private static Icon DrawIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(Color.FromArgb(0x33, 0x99, 0xFF));
        g.FillEllipse(brush, 1, 1, 30, 30);
        using var font = new Font("Segoe UI", 16, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        var size = g.MeasureString("S", font);
        g.DrawString("S", font, Brushes.White, (32 - size.Width) / 2, (32 - size.Height) / 2);
        // the icon handle must outlive the bitmap; Clone detaches it
        return (Icon)Icon.FromHandle(bmp.GetHicon()).Clone();
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
