namespace SymbolCommander.Core.Actions;

public static class ActionValidator
{
    /// <returns>null when valid; otherwise a human-readable error.</returns>
    public static string? Validate(ActionDefinition a)
    {
        if (string.IsNullOrWhiteSpace(a.Name)) return "Action needs a name.";
        switch (a.Type)
        {
            case ActionType.Keystroke:
                try { KeystrokeParser.Parse(a.Get("keys")); }
                catch (FormatException ex) { return ex.Message; }
                return null;
            case ActionType.Launch:
                return string.IsNullOrWhiteSpace(a.Get("target"))
                    ? "Launch action needs a program, file, folder, or URL." : null;
            case ActionType.WindowMedia:
                return Enum.TryParse<WindowMediaCommand>(a.Get("command"), out _)
                    ? null : $"Unknown window/media command \"{a.Get("command")}\".";
            case ActionType.Shell:
                return string.IsNullOrWhiteSpace(a.Get("commandLine"))
                    ? "Shell action needs a command line." : null;
            default:
                return "Unknown action type.";
        }
    }
}
