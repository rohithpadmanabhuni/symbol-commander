namespace SymbolCommander.Core.Actions;

public sealed class ActionDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public ActionType Type { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();

    public string Get(string key) => Parameters.TryGetValue(key, out var v) ? v : "";
}
