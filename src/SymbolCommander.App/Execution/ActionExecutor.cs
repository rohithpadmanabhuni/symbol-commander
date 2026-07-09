using System.Diagnostics;
using SymbolCommander.App.Interop;
using SymbolCommander.Core.Actions;

namespace SymbolCommander.App.Execution;

public sealed class ActionExecutor
{
    public event Action<string>? ActionFailed;

    public void Execute(ActionDefinition action)
    {
        try
        {
            switch (action.Type)
            {
                case ActionType.Keystroke: ExecuteKeystroke(action); break;
                case ActionType.Launch: ExecuteLaunch(action); break;
                case ActionType.WindowMedia: ExecuteWindowMedia(action); break;
                case ActionType.Shell: ExecuteShell(action); break;
            }
        }
        catch (Exception ex)
        {
            ActionFailed?.Invoke($"{action.Name}: {ex.Message}");
        }
    }

    private static void ExecuteKeystroke(ActionDefinition a)
    {
        foreach (var combo in KeystrokeParser.Parse(a.Get("keys")))
            SendCombo(combo);
    }

    private static void SendCombo(KeyCombo combo) =>
        NativeInput.SendCombo(combo.Modifiers.Select(VkMap.ModifierVk).ToArray(), VkMap.KeyVk(combo.Key));

    private static void ExecuteLaunch(ActionDefinition a) =>
        Process.Start(new ProcessStartInfo(a.Get("target")) { UseShellExecute = true });

    private static void ExecuteShell(ActionDefinition a)
    {
        bool hidden = !string.Equals(a.Get("hidden"), "false", StringComparison.OrdinalIgnoreCase);
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c {a.Get("commandLine")}")
        {
            UseShellExecute = false,
            CreateNoWindow = hidden,
        });
    }

    private static void ExecuteWindowMedia(ActionDefinition a)
    {
        var cmd = Enum.Parse<WindowMediaCommand>(a.Get("command"));
        var hwnd = NativeMethods.GetForegroundWindow();
        switch (cmd)
        {
            case WindowMediaCommand.MinimizeWindow: NativeMethods.ShowWindowAsync(hwnd, NativeMethods.SW_MINIMIZE); break;
            case WindowMediaCommand.MaximizeWindow: NativeMethods.ShowWindowAsync(hwnd, NativeMethods.SW_MAXIMIZE); break;
            case WindowMediaCommand.RestoreWindow: NativeMethods.ShowWindowAsync(hwnd, NativeMethods.SW_RESTORE); break;
            case WindowMediaCommand.CloseWindow:
                NativeMethods.PostMessageW(hwnd, NativeMethods.WM_SYSCOMMAND, NativeMethods.SC_CLOSE, 0); break;
            case WindowMediaCommand.SnapLeft: NativeInput.SendCombo(new[] { VkMap.ModifierVk("Win") }, VkMap.KeyVk("Left")); break;
            case WindowMediaCommand.SnapRight: NativeInput.SendCombo(new[] { VkMap.ModifierVk("Win") }, VkMap.KeyVk("Right")); break;
            case WindowMediaCommand.ShowDesktop: NativeInput.SendCombo(new[] { VkMap.ModifierVk("Win") }, VkMap.KeyVk("D")); break;
            case WindowMediaCommand.VolumeUp: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_VOLUME_UP); break;
            case WindowMediaCommand.VolumeDown: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_VOLUME_DOWN); break;
            case WindowMediaCommand.VolumeMute: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_VOLUME_MUTE); break;
            case WindowMediaCommand.MediaPlayPause: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_MEDIA_PLAY_PAUSE); break;
            case WindowMediaCommand.MediaNext: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_MEDIA_NEXT); break;
            case WindowMediaCommand.MediaPrevious: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_MEDIA_PREV); break;
            case WindowMediaCommand.LockWorkstation: NativeMethods.LockWorkStation(); break;
        }
    }
}
