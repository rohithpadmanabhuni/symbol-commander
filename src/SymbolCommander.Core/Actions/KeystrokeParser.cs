namespace SymbolCommander.Core.Actions;

public sealed record KeyCombo(IReadOnlyList<string> Modifiers, string Key);

public static class KeystrokeParser
{
    private static readonly string[] ModifierNames = { "Ctrl", "Alt", "Shift", "Win" };

    public static IReadOnlyList<string> KnownKeys { get; } = BuildKnownKeys();

    private static string[] BuildKnownKeys()
    {
        var keys = new List<string>();
        for (char c = 'A'; c <= 'Z'; c++) keys.Add(c.ToString());
        for (char c = '0'; c <= '9'; c++) keys.Add(c.ToString());
        for (int i = 1; i <= 12; i++) keys.Add($"F{i}");
        keys.AddRange(new[] { "Enter", "Tab", "Space", "Esc", "Backspace", "Delete", "Insert",
            "Home", "End", "PageUp", "PageDown", "Up", "Down", "Left", "Right", "PrintScreen" });
        return keys.ToArray();
    }

    public static IReadOnlyList<KeyCombo> Parse(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
            throw new FormatException("Keystroke is empty.");

        var combos = new List<KeyCombo>();
        foreach (var part in spec.Split(','))
        {
            var tokens = part.Split('+').Select(t => t.Trim()).ToArray();
            if (tokens.Any(t => t.Length == 0))
                throw new FormatException($"Malformed combo: \"{part.Trim()}\"");

            var mods = new List<string>();
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                var m = ModifierNames.FirstOrDefault(n => n.Equals(tokens[i], StringComparison.OrdinalIgnoreCase))
                    ?? throw new FormatException($"Unknown modifier \"{tokens[i]}\" (use Ctrl, Alt, Shift, Win).");
                if (!mods.Contains(m)) mods.Add(m);
            }

            var last = tokens[^1];
            var key = KnownKeys.FirstOrDefault(k => k.Equals(last, StringComparison.OrdinalIgnoreCase))
                ?? throw new FormatException($"Unknown key \"{last}\".");
            combos.Add(new KeyCombo(mods, key));
        }
        return combos;
    }
}
