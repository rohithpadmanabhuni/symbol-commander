using System.IO;
using System.Threading;
using System.Windows;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirst);
        _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        if (!isFirst)
        {
            // another instance is running: ask it to open settings, then quit
            _showSettingsEvent.Set();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SymbolCommander");
        ConfigStore = new ConfigStore(configDir);

        Tray = new TrayIcon();
        Tray.ExitRequested += Shutdown;
        Tray.SettingsRequested += OpenSettings;

        // wake up when a second instance signals us
        var waiter = new Thread(() =>
        {
            while (_showSettingsEvent.WaitOne())
                Dispatcher.BeginInvoke(OpenSettings);
        }) { IsBackground = true };
        waiter.Start();
    }

    private void OpenSettings()
    {
        // Task 13 replaces this with the real settings window
        Tray.ShowNotification("Symbol Commander", "Settings UI arrives in a later task.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Tray?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
