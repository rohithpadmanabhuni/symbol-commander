using System.Text.Json;
using System.Text.Json.Serialization;
using SymbolCommander.Core.Actions;

namespace SymbolCommander.Core.Config;

public sealed class ConfigStore
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _dir;
    private string SymbolsDir => Path.Combine(_dir, "symbols");

    public string ConfigPath => Path.Combine(_dir, "config.json");
    public string Directory { get; }
    public event Action<string>? LoadWarning;

    public ConfigStore(string directory)
    {
        _dir = directory;
        Directory = directory;
    }

    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath)) return DefaultConfig();
        try
        {
            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), JsonOptions);
            return config ?? DefaultConfig();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            var backup = ConfigPath + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            try { File.Copy(ConfigPath, backup, overwrite: true); } catch (IOException) { }
            LoadWarning?.Invoke($"Config file was unreadable and has been reset. Backup: {backup}");
            return DefaultConfig();
        }
    }

    public void Save(AppConfig config)
    {
        System.IO.Directory.CreateDirectory(_dir);
        var tmp = ConfigPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(tmp, ConfigPath, overwrite: true);
    }

    public List<CustomSymbol> LoadCustomSymbols()
    {
        var result = new List<CustomSymbol>();
        if (!System.IO.Directory.Exists(SymbolsDir)) return result;
        foreach (var file in System.IO.Directory.GetFiles(SymbolsDir, "*.json"))
        {
            try
            {
                var sym = JsonSerializer.Deserialize<CustomSymbol>(File.ReadAllText(file), JsonOptions);
                if (sym is not null) result.Add(sym);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                LoadWarning?.Invoke($"Skipped unreadable symbol file {Path.GetFileName(file)}.");
            }
        }
        return result;
    }

    public void SaveCustomSymbol(CustomSymbol s)
    {
        System.IO.Directory.CreateDirectory(SymbolsDir);
        var path = Path.Combine(SymbolsDir, s.Id + ".json");
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(s, JsonOptions));
        File.Move(tmp, path, overwrite: true);
    }

    public void DeleteCustomSymbol(string id)
    {
        var path = Path.Combine(SymbolsDir, id + ".json");
        if (File.Exists(path)) File.Delete(path);
    }

    public static AppConfig DefaultConfig()
    {
        var browser = new ActionDefinition { Name = "Open browser", Type = ActionType.Launch,
            Parameters = { ["target"] = "https://www.google.com" } };
        var minimize = new ActionDefinition { Name = "Minimize window", Type = ActionType.WindowMedia,
            Parameters = { ["command"] = nameof(WindowMediaCommand.MinimizeWindow) } };
        var undo = new ActionDefinition { Name = "Undo", Type = ActionType.Keystroke,
            Parameters = { ["keys"] = "Ctrl+Z" } };
        var volUp = new ActionDefinition { Name = "Volume up", Type = ActionType.WindowMedia,
            Parameters = { ["command"] = nameof(WindowMediaCommand.VolumeUp) } };
        var volDown = new ActionDefinition { Name = "Volume down", Type = ActionType.WindowMedia,
            Parameters = { ["command"] = nameof(WindowMediaCommand.VolumeDown) } };
        var back = new ActionDefinition { Name = "Back", Type = ActionType.Keystroke,
            Parameters = { ["keys"] = "Alt+Left" } };
        var forward = new ActionDefinition { Name = "Forward", Type = ActionType.Keystroke,
            Parameters = { ["keys"] = "Alt+Right" } };
        var showDesktop = new ActionDefinition { Name = "Show desktop", Type = ActionType.WindowMedia,
            Parameters = { ["command"] = nameof(WindowMediaCommand.ShowDesktop) } };

        return new AppConfig
        {
            Actions = { browser, minimize, undo, volUp, volDown, back, forward, showDesktop },
            Bindings =
            {
                new Binding { SymbolId = "w", ActionId = browser.Id },
                new Binding { SymbolId = "m", ActionId = minimize.Id },
                new Binding { SymbolId = "z", ActionId = undo.Id },
                new Binding { SymbolId = "up", ActionId = volUp.Id },
                new Binding { SymbolId = "down", ActionId = volDown.Id },
                new Binding { SymbolId = "left", ActionId = back.Id },
                new Binding { SymbolId = "right", ActionId = forward.Id },
                new Binding { SymbolId = "circle", ActionId = showDesktop.Id },
            },
        };
    }
}
