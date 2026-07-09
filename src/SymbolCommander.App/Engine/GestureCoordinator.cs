using System.Windows;
using SymbolCommander.App.Execution;
using SymbolCommander.App.Interop;
using SymbolCommander.App.Overlay;
using SymbolCommander.App.Voice;
using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Engine;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.App.Engine;

/// <summary>
/// Wires hooks → GestureEngine → recognizer → overlay/executor.
/// Hook callbacks run on the hook thread: they only feed the engine (fast, allocation-light).
/// Engine events marshal to the UI dispatcher for overlay drawing, recognition, and actions.
/// Config-derived state is swapped atomically under _gate for hot reload.
/// </summary>
public sealed class GestureCoordinator : IDisposable
{
    private readonly ConfigStore _store;
    private readonly OverlayWindow _overlay;
    private readonly Action<string, string, bool> _notify; // title, message, warning
    private readonly ActionExecutor _executor = new();
    private readonly GestureEngine _engine = new();
    private readonly MouseHook _mouseHook = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly HookHost _hookHost;

    private readonly object _gate = new();
    private AppConfig _config = null!;
    private SymbolCatalog _catalog = null!;
    private IReadOnlyList<SymbolTemplate> _activeTemplates = Array.Empty<SymbolTemplate>();
    private Dictionary<string, ActionDefinition> _bindingBySymbol = new();
    private HashSet<ushort>[] _hotkeyGroups = Array.Empty<HashSet<ushort>>();

    private readonly HashSet<ushort> _keysDown = new();
    private bool _hotkeyActive;

    private static readonly Dictionary<string, ushort[]> ModifierVariants = new()
    {
        ["Ctrl"] = new ushort[] { 0xA2, 0xA3 },  // L/R VK_CONTROL variants
        ["Alt"] = new ushort[] { 0xA4, 0xA5 },
        ["Shift"] = new ushort[] { 0xA0, 0xA1 },
        ["Win"] = new ushort[] { 0x5B, 0x5C },
    };
    private const ushort VK_ESCAPE = 0x1B;

    public AppConfig CurrentConfig { get { lock (_gate) return _config; } }
    public SymbolCatalog Catalog { get { lock (_gate) return _catalog; } }
    public VoiceService Voice { get; }

    public GestureCoordinator(ConfigStore store, OverlayWindow overlay, Action<string, string, bool> notify)
    {
        _store = store;
        _overlay = overlay;
        _notify = notify;
        Voice = new VoiceService(System.IO.Path.Combine(store.Directory, "voice"), notify);
        _hookHost = new HookHost(_mouseHook, _keyboardHook, () => _engine.State == EngineState.Idle);

        _executor.ActionFailed += msg => OnUi(() => _notify("Action failed", msg, true));
        _store.LoadWarning += msg => OnUi(() => _notify("Symbol Commander", msg, true));

        _engine.TrailStarted += p => OnUi(() => _overlay.StartTrail(p.X, p.Y));
        _engine.TrailPointAdded += p => OnUi(() => _overlay.AddTrailPoint(p.X, p.Y));
        _engine.Cancelled += () => OnUi(_overlay.CancelTrail);
        _engine.StrokeCompleted += stroke => OnUi(() => OnStrokeCompleted(stroke));
        _engine.ClickPassthroughRequested += (p, source) =>
        {
            if (source == TriggerSource.RightButton) NativeInput.SendRightClick();
        };

        _mouseHook.MouseEvent += OnMouseEvent;
        _keyboardHook.KeyEvent += OnKeyEvent;
    }

    public void Start()
    {
        ApplyConfig(_store.Load(), _store.LoadCustomSymbols());
        _hookHost.Start();
        var settings = CurrentConfig.Settings;
        if (settings.VoiceEnabled) Voice.PlayStartup(settings.VoiceVolume);
    }

    public void ApplyConfig(AppConfig config, List<CustomSymbol> customs)
    {
        var catalog = new SymbolCatalog(customs);
        var actionById = config.Actions.ToDictionary(a => a.Id);
        var bindingBySymbol = new Dictionary<string, ActionDefinition>();
        foreach (var b in config.Bindings.Where(b => b.Enabled))
            if (actionById.TryGetValue(b.ActionId, out var action))
                bindingBySymbol[b.SymbolId] = action;

        string[] hotkeyMods;
        try
        {
            hotkeyMods = config.Settings.HotkeyModifiers
                .Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (hotkeyMods.Any(m => !ModifierVariants.ContainsKey(m)))
                throw new FormatException();
        }
        catch (FormatException)
        {
            hotkeyMods = new[] { "Ctrl", "Alt" };
        }

        lock (_gate)
        {
            _config = config;
            _catalog = catalog;
            _bindingBySymbol = bindingBySymbol;
            // recognize only against symbols that have an enabled binding — an unbound
            // symbol should never steal a match from a bound one
            _activeTemplates = catalog.TemplatesFor(bindingBySymbol.Keys);
            _hotkeyGroups = hotkeyMods.Select(m => ModifierVariants[m].ToHashSet()).ToArray();
        }
        OnUi(() => _overlay.ConfigureTrail(config.Settings.TrailColor, config.Settings.TrailThickness));
    }

    public void SetGesturesEnabled(bool on)
    {
        AppConfig config;
        lock (_gate) { _config.Settings.GesturesEnabled = on; config = _config; }
        _store.Save(config);
    }

    public void SetVoiceEnabled(bool on)
    {
        AppConfig config;
        lock (_gate) { _config.Settings.VoiceEnabled = on; config = _config; }
        _store.Save(config);
    }

    // ---- hook thread ----

    private void OnMouseEvent(object? sender, MouseHookEventArgs e)
    {
        if (e.Injected) return;
        bool gesturesOn, rightTriggerOn;
        lock (_gate)
        {
            gesturesOn = _config.Settings.GesturesEnabled;
            rightTriggerOn = _config.Settings.RightButtonTriggerEnabled;
        }
        if (!gesturesOn) return;

        switch (e.Message)
        {
            case MouseHook.WM_RBUTTONDOWN when rightTriggerOn:
                _engine.TriggerDown(new GesturePoint(e.X, e.Y), TriggerSource.RightButton);
                e.Suppress = true;
                break;
            case MouseHook.WM_RBUTTONUP when _engine.ActiveSource == TriggerSource.RightButton:
                _engine.TriggerUp(new GesturePoint(e.X, e.Y));
                e.Suppress = true;
                break;
            case MouseHook.WM_RBUTTONUP when rightTriggerOn && _engine.State == EngineState.Idle:
                // release after an Escape-cancelled right-button gesture
                e.Suppress = true;
                break;
            case MouseHook.WM_MOUSEMOVE:
                _engine.PointerMoved(new GesturePoint(e.X, e.Y));
                break;
        }
    }

    private void OnKeyEvent(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Injected) return;

        if (e.IsDown) _keysDown.Add((ushort)e.VkCode);
        else _keysDown.Remove((ushort)e.VkCode);

        if (e.IsDown && e.VkCode == VK_ESCAPE && _engine.State == EngineState.Drawing)
        {
            _engine.Cancel();
            e.Suppress = true;
            return;
        }

        bool gesturesOn, hotkeyOn;
        HashSet<ushort>[] groups;
        lock (_gate)
        {
            gesturesOn = _config.Settings.GesturesEnabled;
            hotkeyOn = _config.Settings.HotkeyTriggerEnabled;
            groups = _hotkeyGroups;
        }
        if (!gesturesOn || !hotkeyOn || groups.Length == 0) return;

        bool comboHeld = groups.All(g => g.Overlaps(_keysDown));
        if (comboHeld && !_hotkeyActive)
        {
            _hotkeyActive = true;
            var (x, y) = NativeInput.CursorPos();
            _engine.TriggerDown(new GesturePoint(x, y), TriggerSource.Hotkey);
        }
        else if (!comboHeld && _hotkeyActive)
        {
            _hotkeyActive = false;
            if (_engine.ActiveSource == TriggerSource.Hotkey)
            {
                var (x, y) = NativeInput.CursorPos();
                _engine.TriggerUp(new GesturePoint(x, y));
            }
        }
    }

    // ---- UI thread ----

    private void OnStrokeCompleted(IReadOnlyList<GesturePoint> stroke)
    {
        IReadOnlyList<SymbolTemplate> templates;
        Dictionary<string, ActionDefinition> bindings;
        double threshold;
        SymbolCatalog catalog;
        bool voiceOn;
        double voiceVolume;
        lock (_gate)
        {
            templates = _activeTemplates;
            bindings = _bindingBySymbol;
            threshold = _config.Settings.Sensitivity;
            catalog = _catalog;
            voiceOn = _config.Settings.VoiceEnabled;
            voiceVolume = _config.Settings.VoiceVolume;
        }

        var result = ProtractorRecognizer.Recognize(stroke, templates, threshold);
        if (result.IsMatch && bindings.TryGetValue(result.SymbolId!, out var action))
        {
            _overlay.EndTrailRecognized($"{catalog.NameOf(result.SymbolId!)}  →  {action.Name}");
            _executor.Execute(action);
            if (voiceOn) Voice.PlayForAction(action, voiceVolume);
        }
        else
        {
            _overlay.EndTrailRejected();
        }
    }

    private static void OnUi(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;
        if (dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }

    public void Dispose()
    {
        _hookHost.Dispose();
        _mouseHook.Dispose();
        _keyboardHook.Dispose();
    }
}
