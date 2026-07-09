namespace SymbolCommander.App.Execution;

/// <summary>Key name (KeystrokeParser.KnownKeys) → Windows virtual-key code.</summary>
public static class VkMap
{
    private static readonly Dictionary<string, ushort> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = 0x0D, ["Tab"] = 0x09, ["Space"] = 0x20, ["Esc"] = 0x1B,
        ["Backspace"] = 0x08, ["Delete"] = 0x2E, ["Insert"] = 0x2D,
        ["Home"] = 0x24, ["End"] = 0x23, ["PageUp"] = 0x21, ["PageDown"] = 0x22,
        ["Up"] = 0x26, ["Down"] = 0x28, ["Left"] = 0x25, ["Right"] = 0x27,
        ["PrintScreen"] = 0x2C,
    };

    public static ushort KeyVk(string keyName)
    {
        if (keyName.Length == 1 && keyName[0] is (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
            return (ushort)keyName[0];
        if (keyName.Length is 2 or 3 && keyName[0] is 'F' or 'f'
            && int.TryParse(keyName[1..], out int f) && f is >= 1 and <= 12)
            return (ushort)(0x70 + f - 1);
        if (Named.TryGetValue(keyName, out var vk)) return vk;
        throw new ArgumentException($"No virtual key for \"{keyName}\".");
    }

    public static ushort ModifierVk(string modifierName) => modifierName switch
    {
        "Ctrl" => 0x11,  // VK_CONTROL
        "Alt" => 0x12,   // VK_MENU
        "Shift" => 0x10, // VK_SHIFT
        "Win" => 0x5B,   // VK_LWIN
        _ => throw new ArgumentException($"No virtual key for modifier \"{modifierName}\"."),
    };

    // media/volume VKs used by ActionExecutor
    public const ushort VK_VOLUME_MUTE = 0xAD;
    public const ushort VK_VOLUME_DOWN = 0xAE;
    public const ushort VK_VOLUME_UP = 0xAF;
    public const ushort VK_MEDIA_NEXT = 0xB0;
    public const ushort VK_MEDIA_PREV = 0xB1;
    public const ushort VK_MEDIA_PLAY_PAUSE = 0xB3;
}
