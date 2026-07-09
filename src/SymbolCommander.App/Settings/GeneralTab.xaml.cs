using System.Windows.Controls;
using SymbolCommander.Core.Config;

namespace SymbolCommander.App.Settings;

public partial class GeneralTab : UserControl
{
    private SettingsWindow _owner = null!;
    private static readonly string[] HotkeyPresets =
        { "Ctrl+Alt", "Ctrl+Shift", "Alt+Shift", "Ctrl+Alt+Shift", "Win+Alt" };

    public GeneralTab()
    {
        InitializeComponent();
        foreach (var p in HotkeyPresets) HotkeyCombo.Items.Add(p);
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Blue", Tag = "#3399FF" });
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Red", Tag = "#E74C3C" });
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Green", Tag = "#2ECC71" });
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Orange", Tag = "#E67E22" });
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Purple", Tag = "#9B59B6" });
        SensitivitySlider.ValueChanged += (_, _) => SensitivityLabel.Text = SensitivitySlider.Value.ToString("0.00");
    }

    public void Load(SettingsWindow owner)
    {
        _owner = owner;
        var s = owner.Working.Settings;
        RightButtonCheck.IsChecked = s.RightButtonTriggerEnabled;
        HotkeyCheck.IsChecked = s.HotkeyTriggerEnabled;
        HotkeyCombo.SelectedItem = HotkeyPresets.Contains(s.HotkeyModifiers) ? s.HotkeyModifiers : HotkeyPresets[0];
        SensitivitySlider.Value = s.Sensitivity;
        SensitivityLabel.Text = s.Sensitivity.ToString("0.00");
        TrailColorCombo.SelectedValue = s.TrailColor;
        if (TrailColorCombo.SelectedIndex < 0) TrailColorCombo.SelectedIndex = 0;
        ThicknessSlider.Value = s.TrailThickness;
        StartupCheck.IsChecked = s.StartWithWindows;
        EnabledCheck.IsChecked = s.GesturesEnabled;
        RefreshActions();
    }

    public void Reload() => RefreshActions();

    public void CollectInto(AppConfig working)
    {
        var s = working.Settings;
        s.RightButtonTriggerEnabled = RightButtonCheck.IsChecked == true;
        s.HotkeyTriggerEnabled = HotkeyCheck.IsChecked == true;
        s.HotkeyModifiers = HotkeyCombo.SelectedItem as string ?? "Ctrl+Alt";
        s.Sensitivity = SensitivitySlider.Value;
        s.TrailColor = TrailColorCombo.SelectedValue as string ?? "#3399FF";
        s.TrailThickness = ThicknessSlider.Value;
        s.StartWithWindows = StartupCheck.IsChecked == true;
        s.GesturesEnabled = EnabledCheck.IsChecked == true;
    }

    // Task 14 adds the Actions list logic (RefreshActions + Add/Edit/Remove handlers).
    private void RefreshActions() { }
}
