using System.Runtime.InteropServices;

namespace SymbolCommander.App.Interop;

using static NativeMethods;

public sealed class KeyboardHookEventArgs : EventArgs
{
    public required uint VkCode { get; init; }
    public required bool IsDown { get; init; }
    public required bool Injected { get; init; }
    public bool Suppress { get; set; }
}

public sealed class KeyboardHook : IDisposable
{
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    public event EventHandler<KeyboardHookEventArgs>? KeyEvent;

    private IntPtr _handle;
    private HookProc? _proc;

    public void Install()
    {
        if (_handle != IntPtr.Zero) return;
        _proc = Callback;
        _handle = SetWindowsHookExW(WH_KEYBOARD_LL, _proc, GetModuleHandleW(null), 0);
        if (_handle == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "SetWindowsHookEx(keyboard) failed");
    }

    public void Uninstall()
    {
        if (_handle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_handle);
        _handle = IntPtr.Zero;
        _proc = null;
    }

    private IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && KeyEvent is not null)
        {
            int msg = (int)wParam;
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var args = new KeyboardHookEventArgs
            {
                VkCode = data.vkCode,
                IsDown = msg is WM_KEYDOWN or WM_SYSKEYDOWN,
                Injected = (data.flags & 0x10) != 0,
            };
            KeyEvent(this, args);
            if (args.Suppress) return 1;
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
