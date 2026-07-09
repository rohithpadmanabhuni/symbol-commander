namespace SymbolCommander.Core.Config;

public sealed class AppSettings
{
    public bool GesturesEnabled { get; set; } = true;
    public bool RightButtonTriggerEnabled { get; set; } = true;
    public bool HotkeyTriggerEnabled { get; set; } = true;
    public string HotkeyModifiers { get; set; } = "Ctrl+Alt";
    public double Sensitivity { get; set; } = 0.80;
    public string TrailColor { get; set; } = "#3399FF";
    public double TrailThickness { get; set; } = 4.0;
    public bool StartWithWindows { get; set; }
}
