using System.Windows;
using System.Windows.Controls;
using SymbolCommander.Core.Actions;

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
        var action = new ActionDefinition { Id = _id, Name = NameBox.Text.Trim(), Type = t };
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
