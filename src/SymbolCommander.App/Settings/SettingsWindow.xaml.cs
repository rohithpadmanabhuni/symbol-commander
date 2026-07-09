using System.Windows;
using System.Windows.Controls;
using SymbolCommander.App.Engine;
using SymbolCommander.Core.Config;

namespace SymbolCommander.App.Settings;

public partial class SettingsWindow : Window
{
    public AppConfig Working { get; }
    public ConfigStore Store { get; }
    public GestureCoordinator Coordinator { get; }

    // Apply is enabled only while unsaved changes are pending. Tabs call MarkDirty()
    // on user edits; programmatic loads/reloads run under _suppressDirty so they don't count.
    private bool _suppressDirty;

    public SettingsWindow(GestureCoordinator coordinator, ConfigStore store)
    {
        InitializeComponent();
        Coordinator = coordinator;
        Store = store;
        Working = coordinator.CurrentConfig.Clone();
        WithoutDirty(() =>
        {
            General.Load(this);
            Bindings.Load(this);
            Symbols.Load(this);
        });
        SetClean();
    }

    /// <summary>Called by tabs when the user changes something. Enables Apply.</summary>
    public void MarkDirty()
    {
        if (_suppressDirty) return;
        ApplyButton.IsEnabled = true;
    }

    private void SetClean() => ApplyButton.IsEnabled = false;

    private void WithoutDirty(Action action)
    {
        bool prev = _suppressDirty;
        _suppressDirty = true;
        try { action(); }
        finally { _suppressDirty = prev; }
    }

    public void ApplyWorking()
    {
        General.CollectInto(Working);
        Bindings.CollectInto(Working);
        // Task 16: Symbols tab saves customs directly, no CollectInto needed
        var snapshot = Working.Clone();
        Store.Save(snapshot);
        Coordinator.ApplyConfig(snapshot, Store.LoadCustomSymbols());
        StartupManager.Apply(snapshot.Settings.StartWithWindows);
        App.Current.Tray.SetGesturesEnabled(snapshot.Settings.GesturesEnabled);
        SetClean();
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, Tabs)) return;
        // Reload rebuilds control state from Working; that must not count as a user edit.
        WithoutDirty(() =>
        {
            if (Tabs.SelectedItem is TabItem { Content: BindingsTab bt }) bt.Reload();
            else if (Tabs.SelectedItem is TabItem { Content: SymbolsTab st }) st.Reload();
            else if (Tabs.SelectedItem is TabItem { Content: GeneralTab gt }) gt.Reload();
        });
    }

    private void Ok_Click(object sender, RoutedEventArgs e) { ApplyWorking(); Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    private void Apply_Click(object sender, RoutedEventArgs e) => ApplyWorking();
}
