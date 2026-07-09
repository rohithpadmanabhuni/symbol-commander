using System.Text.Json;
using SymbolCommander.Core.Actions;

namespace SymbolCommander.Core.Config;

public sealed class AppConfig
{
    public int SchemaVersion { get; set; } = 1;
    public AppSettings Settings { get; set; } = new();
    public List<ActionDefinition> Actions { get; set; } = new();
    public List<Binding> Bindings { get; set; } = new();

    public AppConfig Clone() =>
        JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(this, ConfigStore.JsonOptions), ConfigStore.JsonOptions)!;
}
