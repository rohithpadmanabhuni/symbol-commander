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

    public SettingsWindow(GestureCoordinator coordinator, ConfigStore store)
    {
        InitializeComponent();
        Coordinator = coordinator;
        Store = store;
        Working = coordinator.CurrentConfig.Clone();
        General.Load(this);
        Bindings.Load(this);
        Symbols.Load(this);
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
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, Tabs)) return;
        if (Tabs.SelectedItem is TabItem { Content: BindingsTab bt }) bt.Reload();
        else if (Tabs.SelectedItem is TabItem { Content: SymbolsTab st }) st.Reload();
        else if (Tabs.SelectedItem is TabItem { Content: GeneralTab gt }) gt.Reload();
    }

    private void Ok_Click(object sender, RoutedEventArgs e) { ApplyWorking(); Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    private void Apply_Click(object sender, RoutedEventArgs e) => ApplyWorking();
}
