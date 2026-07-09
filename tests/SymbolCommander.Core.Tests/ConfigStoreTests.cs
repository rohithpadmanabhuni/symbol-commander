using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Library;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sc-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Fact]
    public void Load_with_no_file_returns_defaults()
    {
        var store = new ConfigStore(_dir);
        var config = store.Load();
        Assert.Equal(1, config.SchemaVersion);
        Assert.True(config.Settings.GesturesEnabled);
        Assert.NotEmpty(config.Actions);
        Assert.NotEmpty(config.Bindings);
    }

    [Fact]
    public void Save_then_load_round_trips_everything()
    {
        var store = new ConfigStore(_dir);
        var config = ConfigStore.DefaultConfig();
        config.Settings.Sensitivity = 0.66;
        config.Settings.HotkeyModifiers = "Ctrl+Shift";
        config.Actions.Add(new ActionDefinition
        {
            Name = "Notepad",
            Type = ActionType.Launch,
            Parameters = { ["target"] = @"C:\Windows\notepad.exe" },
        });
        store.Save(config);

        var loaded = new ConfigStore(_dir).Load();
        Assert.Equal(0.66, loaded.Settings.Sensitivity);
        Assert.Equal("Ctrl+Shift", loaded.Settings.HotkeyModifiers);
        Assert.Contains(loaded.Actions, a => a.Name == "Notepad" && a.Type == ActionType.Launch
            && a.Get("target") == @"C:\Windows\notepad.exe");
        Assert.Equal(config.Bindings.Count, loaded.Bindings.Count);
    }

    [Fact]
    public void Save_is_atomic_no_tmp_file_left_behind()
    {
        var store = new ConfigStore(_dir);
        store.Save(ConfigStore.DefaultConfig());
        Assert.True(File.Exists(store.ConfigPath));
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void Corrupt_config_backs_up_warns_and_returns_defaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "config.json"), "{ this is not json !!");
        var store = new ConfigStore(_dir);
        string? warning = null;
        store.LoadWarning += w => warning = w;

        var config = store.Load();

        Assert.NotNull(warning);
        Assert.NotEmpty(config.Actions); // defaults
        Assert.NotEmpty(Directory.GetFiles(_dir, "config.json.corrupt-*"));
    }

    [Fact]
    public void Custom_symbols_save_load_delete()
    {
        var store = new ConfigStore(_dir);
        var sym = new CustomSymbol
        {
            Name = "Star",
            Examples = { new List<GesturePoint> { new(0, 0), new(10, 10), new(20, 0) } },
        };
        store.SaveCustomSymbol(sym);

        var loaded = new ConfigStore(_dir).LoadCustomSymbols();
        var s = Assert.Single(loaded);
        Assert.Equal("Star", s.Name);
        Assert.Equal(sym.Id, s.Id);
        Assert.Equal(new GesturePoint(10, 10), s.Examples[0][1]);

        store.DeleteCustomSymbol(sym.Id);
        Assert.Empty(new ConfigStore(_dir).LoadCustomSymbols());
    }

    [Fact]
    public void Default_config_is_internally_consistent()
    {
        var config = ConfigStore.DefaultConfig();
        var actionIds = config.Actions.Select(a => a.Id).ToHashSet();
        var builtInIds = BuiltInSymbols.All.Select(s => s.Id).ToHashSet();
        foreach (var b in config.Bindings)
        {
            Assert.Contains(b.ActionId, actionIds);
            Assert.Contains(b.SymbolId, builtInIds);
        }
        foreach (var a in config.Actions)
            Assert.Null(ActionValidator.Validate(a));
    }

    [Fact]
    public void Clone_is_deep()
    {
        var config = ConfigStore.DefaultConfig();
        var clone = config.Clone();
        clone.Settings.Sensitivity = 0.11;
        clone.Actions[0].Name = "mutated";
        Assert.NotEqual(0.11, config.Settings.Sensitivity);
        Assert.NotEqual("mutated", config.Actions[0].Name);
    }
}
