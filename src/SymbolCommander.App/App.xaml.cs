using System.IO;
using System.Threading;
using System.Windows;
using SymbolCommander.App.Engine;
using SymbolCommander.App.Overlay;
using SymbolCommander.App.Tray;
using SymbolCommander.Core.Config;

namespace SymbolCommander.App;

public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\SymbolCommander.SingleInstance";
    private const string ShowSettingsEventName = @"Local\SymbolCommander.ShowSettings";

    private Mutex? _instanceMutex;
    private EventWaitHandle? _showSettingsEvent;

    public static new App Current => (App)System.Windows.Application.Current;
    public ConfigStore ConfigStore { get; private set; } = null!;
    public TrayIcon Tray { get; private set; } = null!;
    public GestureCoordinator Coordinator { get; private set; } = null!;
    public OverlayWindow Overlay { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirst);
        _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        if (!isFirst)
        {
            _showSettingsEvent.Set();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SymbolCommander");
        ConfigStore = new ConfigStore(configDir);

        Tray = new TrayIcon();
        Overlay = new OverlayWindow();
        Overlay.Show();

        Coordinator = new GestureCoordinator(ConfigStore, Overlay, Tray.ShowNotification);
        Coordinator.Start();
        Tray.SetGesturesEnabled(Coordinator.CurrentConfig.Settings.GesturesEnabled);
        Tray.SetVoiceEnabled(Coordinator.CurrentConfig.Settings.VoiceEnabled);

        Tray.ExitRequested += Shutdown;
        Tray.SettingsRequested += OpenSettings;
        Tray.GesturesToggled += on => Coordinator.SetGesturesEnabled(on);
        Tray.VoiceToggled += on => Coordinator.SetVoiceEnabled(on);

        var waiter = new Thread(() =>
        {
            while (_showSettingsEvent.WaitOne())
                Dispatcher.BeginInvoke(OpenSettings);
        }) { IsBackground = true };
        waiter.Start();
    }

    private Settings.SettingsWindow? _settingsWindow;

    private void OpenSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new Settings.SettingsWindow(Coordinator, ConfigStore);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Coordinator?.Dispose();
        Tray?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
