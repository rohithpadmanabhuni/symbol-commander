using System.Windows;
using System.Windows.Controls;
using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Voice;

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

        // Any user edit lights up the Apply button (suppressed during Load via the owner).
        RightButtonCheck.Click += (_, _) => _owner?.MarkDirty();
        HotkeyCheck.Click += (_, _) => _owner?.MarkDirty();
        StartupCheck.Click += (_, _) => _owner?.MarkDirty();
        EnabledCheck.Click += (_, _) => _owner?.MarkDirty();
        HotkeyCombo.SelectionChanged += (_, _) => _owner?.MarkDirty();
        TrailColorCombo.SelectionChanged += (_, _) => _owner?.MarkDirty();
        SensitivitySlider.ValueChanged += (_, _) => _owner?.MarkDirty();
        ThicknessSlider.ValueChanged += (_, _) => _owner?.MarkDirty();
        VoiceCheck.Click += (_, _) => _owner?.MarkDirty();
        VoiceVolumeSlider.ValueChanged += (_, _) => _owner?.MarkDirty();
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
        VoiceCheck.IsChecked = s.VoiceEnabled;
        VoiceVolumeSlider.Value = s.VoiceVolume;
        UpdateVoiceStatus();
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
        s.VoiceEnabled = VoiceCheck.IsChecked == true;
        s.VoiceVolume = VoiceVolumeSlider.Value;
    }

    private void ImportZip_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
            { Filter = "Zip archives|*.zip", Title = "Import voice pack" };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;
        RunImport(() => VoicePackImporter.ImportZip(dlg.FileName, _owner.Coordinator.Voice.VoiceDir));
    }

    private void ImportFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Import voice pack folder" };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;
        RunImport(() => VoicePackImporter.ImportFolder(dlg.FolderName, _owner.Coordinator.Voice.VoiceDir));
    }

    private void RunImport(Func<int> import)
    {
        try
        {
            int n = import();
            UpdateVoiceStatus();
            MessageBox.Show($"Imported {n} voice sample(s).", "Symbol Commander",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateVoiceStatus() =>
        VoiceStatusLabel.Text = $"{_owner.Coordinator.Voice.AvailableFiles().Count} voice samples available";

    private sealed record ActionRow(ActionDefinition Action)
    {
        public string Display => $"{Action.Name}  ({Action.Type})";
    }

    private void RefreshActions()
    {
        ActionsList.Items.Clear();
        foreach (var a in _owner.Working.Actions) ActionsList.Items.Add(new ActionRow(a));
    }

    private void AddAction_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ActionEditorDialog(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result is { } action)
        {
            _owner.Working.Actions.Add(action);
            RefreshActions();
            _owner.MarkDirty();
        }
    }

    private void EditAction_Click(object sender, RoutedEventArgs e)
    {
        if (ActionsList.SelectedItem is not ActionRow row) return;
        var dlg = new ActionEditorDialog(row.Action) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result is { } edited)
        {
            var i = _owner.Working.Actions.FindIndex(a => a.Id == edited.Id);
            if (i >= 0) _owner.Working.Actions[i] = edited;
            RefreshActions();
            _owner.MarkDirty();
        }
    }

    private void RemoveAction_Click(object sender, RoutedEventArgs e)
    {
        if (ActionsList.SelectedItem is not ActionRow row) return;
        int used = _owner.Working.Bindings.RemoveAll(b => b.ActionId == row.Action.Id);
        if (used > 0)
            MessageBox.Show($"Also removed {used} binding(s) that used \"{row.Action.Name}\".",
                "Symbol Commander", MessageBoxButton.OK, MessageBoxImage.Information);
        _owner.Working.Actions.Remove(row.Action);
        RefreshActions();
        _owner.MarkDirty();
    }
}
