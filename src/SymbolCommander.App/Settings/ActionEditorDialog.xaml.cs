using System.IO;
using System.Windows;
using System.Windows.Controls;
using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Voice;

namespace SymbolCommander.App.Settings;

public partial class ActionEditorDialog : Window
{
    public ActionDefinition? Result { get; private set; }
    private readonly string _id;

    public ActionEditorDialog(ActionDefinition? existing)
    {
        InitializeComponent();
        foreach (var t in Enum.GetValues<ActionType>()) TypeCombo.Items.Add(t);
        foreach (var c in Enum.GetValues<WindowMediaCommand>()) CommandCombo.Items.Add(c);
        CommandCombo.SelectedIndex = 0;

        _id = existing?.Id ?? Guid.NewGuid().ToString("N");
        NameBox.Text = existing?.Name ?? "";
        TypeCombo.SelectedItem = existing?.Type ?? ActionType.Keystroke;
        if (existing is not null)
        {
            KeysBox.Text = existing.Get("keys");
            TargetBox.Text = existing.Get("target");
            if (Enum.TryParse<WindowMediaCommand>(existing.Get("command"), out var cmd))
                CommandCombo.SelectedItem = cmd;
            CommandLineBox.Text = existing.Get("commandLine");
            HiddenCheck.IsChecked = !string.Equals(existing.Get("hidden"), "false", StringComparison.OrdinalIgnoreCase);
        }
        UpdatePanels();
        RefreshVoiceChoices(existing?.Voice ?? "");
    }

    private sealed record VoiceChoice(string Value, string Display)
    {
        public override string ToString() => Display;
    }

    private string SelectedVoiceValue() =>
        (VoiceCombo.SelectedItem as VoiceChoice)?.Value ?? "";

    private void RefreshVoiceChoices(string select)
    {
        var svc = App.Current.Coordinator.Voice;
        var files = svc.AvailableFiles();

        var autoProbe = new ActionDefinition { Name = NameBox.Text.Trim(), Voice = "" };
        var auto = VoiceResolver.Resolve(autoProbe, files);
        string autoDisplay = auto.Kind == VoiceResolutionKind.File
            ? $"Auto (→ {auto.FileName})"
            : $"Auto (→ TTS: \"{autoProbe.Name}\")";

        var choices = new List<VoiceChoice>
        {
            new("", autoDisplay),
            new(VoiceResolver.NoneValue, "None"),
        };
        choices.AddRange(files.Select(f => new VoiceChoice(f, f)));
        if (select is not ("" or VoiceResolver.NoneValue)
            && !files.Contains(select, StringComparer.OrdinalIgnoreCase))
            choices.Add(new VoiceChoice(select, $"{select} (missing)"));

        VoiceCombo.ItemsSource = choices;
        VoiceCombo.SelectedItem =
            choices.FirstOrDefault(c => c.Value.Equals(select, StringComparison.OrdinalIgnoreCase))
            ?? choices[0];
    }

    private void PreviewVoice_Click(object sender, RoutedEventArgs e)
    {
        var svc = App.Current.Coordinator.Voice;
        double volume = App.Current.Coordinator.CurrentConfig.Settings.VoiceVolume;
        var probe = new ActionDefinition { Name = NameBox.Text.Trim(), Voice = SelectedVoiceValue() };
        var r = VoiceResolver.Resolve(probe, svc.AvailableFiles());
        if (r.Kind == VoiceResolutionKind.File)
        {
            svc.PlayFile(r.FileName!, volume);
        }
        else if (r.Kind == VoiceResolutionKind.TtsNeeded)
        {
            var rel = svc.TryGenerateTts(r.TtsText!);
            if (rel is not null)
            {
                svc.PlayFile(rel, volume);
                RefreshVoiceChoices(SelectedVoiceValue());
            }
        }
    }

    private void GenerateTts_Click(object sender, RoutedEventArgs e)
    {
        var rel = App.Current.Coordinator.Voice.TryGenerateTts(NameBox.Text.Trim());
        if (rel is not null) RefreshVoiceChoices(select: rel);
    }

    private void AddVoiceFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
            { Filter = "Audio files|*.mp3;*.wav", Title = "Add voice sample" };
        if (dlg.ShowDialog(this) != true) return;
        var svc = App.Current.Coordinator.Voice;
        Directory.CreateDirectory(svc.VoiceDir);
        var name = Path.GetFileName(dlg.FileName);
        File.Copy(dlg.FileName, Path.Combine(svc.VoiceDir, name), overwrite: true);
        RefreshVoiceChoices(select: name);
    }

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePanels();

    private void UpdatePanels()
    {
        var t = (ActionType?)TypeCombo.SelectedItem ?? ActionType.Keystroke;
        KeystrokePanel.Visibility = t == ActionType.Keystroke ? Visibility.Visible : Visibility.Collapsed;
        LaunchPanel.Visibility = t == ActionType.Launch ? Visibility.Visible : Visibility.Collapsed;
        WindowMediaPanel.Visibility = t == ActionType.WindowMedia ? Visibility.Visible : Visibility.Collapsed;
        ShellPanel.Visibility = t == ActionType.Shell ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Choose a program or file" };
        if (dlg.ShowDialog(this) == true) TargetBox.Text = dlg.FileName;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var t = (ActionType?)TypeCombo.SelectedItem ?? ActionType.Keystroke;
        var action = new ActionDefinition
            { Id = _id, Name = NameBox.Text.Trim(), Type = t, Voice = SelectedVoiceValue() };
        switch (t)
        {
            case ActionType.Keystroke: action.Parameters["keys"] = KeysBox.Text.Trim(); break;
            case ActionType.Launch: action.Parameters["target"] = TargetBox.Text.Trim(); break;
            case ActionType.WindowMedia: action.Parameters["command"] = CommandCombo.SelectedItem?.ToString() ?? ""; break;
            case ActionType.Shell:
                action.Parameters["commandLine"] = CommandLineBox.Text.Trim();
                action.Parameters["hidden"] = HiddenCheck.IsChecked == true ? "true" : "false";
                break;
        }
        var error = ActionValidator.Validate(action);
        if (error is not null)
        {
            ErrorLabel.Text = error;
            ErrorLabel.Visibility = Visibility.Visible;
            return;
        }
        Result = action;
        DialogResult = true;
    }
}
