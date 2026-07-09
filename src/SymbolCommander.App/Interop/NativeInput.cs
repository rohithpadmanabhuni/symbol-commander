namespace SymbolCommander.App.Interop;

using static NativeMethods;

/// <summary>SendInput helpers. Everything sent here carries the injected flag,
/// which our own hooks check and skip — no feedback loops.</summary>
public static class NativeInput
{
    public static (int X, int Y) CursorPos()
    {
        NativeMethods.GetCursorPos(out var p);
        return (p.X, p.Y);
    }

    public static void SendCombo(IReadOnlyList<ushort> modifierVks, ushort keyVk)
    {
        var inputs = new List<INPUT>();
        foreach (var m in modifierVks) inputs.Add(Key(m, down: true));
        inputs.Add(Key(keyVk, down: true));
        inputs.Add(Key(keyVk, down: false));
        for (int i = modifierVks.Count - 1; i >= 0; i--) inputs.Add(Key(modifierVks[i], down: false));
        Send(inputs.ToArray());
    }

    public static void SendRightClick()
    {
        Send(new[]
        {
            new INPUT { type = INPUT_MOUSE, U = new INPUTUNION { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTDOWN } } },
            new INPUT { type = INPUT_MOUSE, U = new INPUTUNION { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTUP } } },
        });
    }

    private static INPUT Key(ushort vk, bool down) => new()
    {
        type = INPUT_KEYBOARD,
        U = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = down ? 0u : KEYEVENTF_KEYUP } },
    };

    private static void Send(INPUT[] inputs) =>
        SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
}
