using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Config;

public sealed class CustomSymbol
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public List<List<GesturePoint>> Examples { get; set; } = new();
}
