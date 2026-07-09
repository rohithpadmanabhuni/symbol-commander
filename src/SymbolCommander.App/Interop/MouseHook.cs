using System.Runtime.InteropServices;

namespace SymbolCommander.App.Interop;

using static NativeMethods;

public sealed class MouseHookEventArgs : EventArgs
{
    public required int Message { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required bool Injected { get; init; }
    public bool Suppress { get; set; }
}

/// <summary>WH_MOUSE_LL wrapper. Install/Uninstall must be called on a thread
/// that pumps messages (HookHost provides one). Keep handlers FAST — Windows
/// silently removes hooks whose callbacks stall.</summary>
public sealed class MouseHook : IDisposable
{
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;

    public event EventHandler<MouseHookEventArgs>? MouseEvent;

    private IntPtr _handle;
    private HookProc? _proc; // field keeps the delegate alive; a local would be GC'd under us

    public void Install()
    {
        if (_handle != IntPtr.Zero) return;
        _proc = Callback;
        _handle = SetWindowsHookExW(WH_MOUSE_LL, _proc, GetModuleHandleW(null), 0);
        if (_handle == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "SetWindowsHookEx(mouse) failed");
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
        if (nCode >= 0 && MouseEvent is not null)
        {
            int msg = (int)wParam;
            if (msg is WM_MOUSEMOVE or WM_RBUTTONDOWN or WM_RBUTTONUP)
            {
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var args = new MouseHookEventArgs
                {
                    Message = msg, X = data.pt.X, Y = data.pt.Y,
                    Injected = (data.flags & 0x1) != 0,
                };
                MouseEvent(this, args);
                if (args.Suppress) return 1;
            }
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
