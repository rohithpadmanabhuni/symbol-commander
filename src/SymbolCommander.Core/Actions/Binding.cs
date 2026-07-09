namespace SymbolCommander.Core.Actions;

public sealed class Binding
{
    public string SymbolId { get; set; } = "";
    public string ActionId { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
