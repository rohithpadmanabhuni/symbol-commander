using System.Windows.Threading;

namespace SymbolCommander.App.Interop;

/// <summary>Runs both hooks on a dedicated message-pumping thread. A 60s watchdog
/// re-installs them (Windows silently drops hooks it considers slow); reinstall is
/// skipped while a gesture is in progress via the reinstallAllowed callback.</summary>
public sealed class HookHost : IDisposable
{
    private readonly MouseHook _mouse;
    private readonly KeyboardHook _keyboard;
    private readonly Func<bool> _reinstallAllowed;
    private Dispatcher? _dispatcher;
    private Thread? _thread;

    public HookHost(MouseHook mouse, KeyboardHook keyboard, Func<bool> reinstallAllowed)
    {
        _mouse = mouse;
        _keyboard = keyboard;
        _reinstallAllowed = reinstallAllowed;
    }

    public void Start()
    {
        if (_thread is not null) return;
        var ready = new ManualResetEventSlim();
        _thread = new Thread(() =>
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _mouse.Install();
            _keyboard.Install();

            var watchdog = new DispatcherTimer(TimeSpan.FromSeconds(60), DispatcherPriority.Normal, (_, _) =>
            {
                if (!_reinstallAllowed()) return;
                _mouse.Uninstall(); _mouse.Install();
                _keyboard.Uninstall(); _keyboard.Install();
            }, _dispatcher);
            watchdog.Start();

            ready.Set();
            Dispatcher.Run();

            _mouse.Uninstall();
            _keyboard.Uninstall();
        }) { IsBackground = true, Name = "SymbolCommander.Hooks" };
        _thread.Start();
        ready.Wait(TimeSpan.FromSeconds(5));
    }

    public void Stop()
    {
        // cross-thread: InvokeShutdown() would throw; BeginInvokeShutdown queues it
        _dispatcher?.BeginInvokeShutdown(DispatcherPriority.Normal);
        _thread?.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _dispatcher = null;
    }

    public void Dispose() => Stop();
}
