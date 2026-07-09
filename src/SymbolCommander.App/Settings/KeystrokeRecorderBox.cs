using System.Windows.Controls;
using System.Windows.Input;

namespace SymbolCommander.App.Settings;

/// <summary>TextBox that records a pressed key combo as text ("Ctrl+Shift+T").
/// Text stays editable by hand so users can type sequences ("Ctrl+K, Ctrl+S").</summary>
public sealed class KeystrokeRecorderBox : TextBox
{
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            e.Handled = true;
            return;
        }
        if (key == Key.Tab) return; // keep keyboard navigation working

        var name = MapKey(key);
        if (name is null) { base.OnPreviewKeyDown(e); return; }

        var parts = new List<string>();
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(name);
        Text = string.Join("+", parts);
        CaretIndex = Text.Length;
        e.Handled = true;
    }

    private static string? MapKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.D0 and <= Key.D9 => key.ToString()[1..],
        >= Key.F1 and <= Key.F12 => key.ToString(),
        Key.Return => "Enter", Key.Space => "Space", Key.Escape => "Esc",
        Key.Back => "Backspace", Key.Delete => "Delete", Key.Insert => "Insert",
        Key.Home => "Home", Key.End => "End", Key.PageUp => "PageUp", Key.Next => "PageDown",
        Key.Up => "Up", Key.Down => "Down", Key.Left => "Left", Key.Right => "Right",
        _ => null,
    };
}
