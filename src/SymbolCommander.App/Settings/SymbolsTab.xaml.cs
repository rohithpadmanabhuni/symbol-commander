using System.Windows;
using System.Windows.Controls;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Library;

namespace SymbolCommander.App.Settings;

public partial class SymbolsTab : UserControl
{
    private SettingsWindow _owner = null!;

    public SymbolsTab() => InitializeComponent();

    public void Load(SettingsWindow owner)
    {
        _owner = owner;
        foreach (var s in BuiltInSymbols.All) BuiltInList.Items.Add(s.Name);
        Reload();
    }

    public void Reload()
    {
        CustomList.Items.Clear();
        foreach (var s in _owner.Store.LoadCustomSymbols()) CustomList.Items.Add(s);
    }

    public void CollectInto(AppConfig working) { } // customs persist directly via ConfigStore

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SymbolEditorDialog(_owner.Store) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            Reload();
            // make the new symbol usable immediately, without waiting for OK/Apply
            _owner.Coordinator.ApplyConfig(_owner.Coordinator.CurrentConfig, _owner.Store.LoadCustomSymbols());
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (CustomList.SelectedItem is not CustomSymbol symbol) return;
        int used = _owner.Working.Bindings.Count(b => b.SymbolId == symbol.Id);
        var msg = used > 0
            ? $"Delete \"{symbol.Name}\"? {used} binding(s) using it will also be removed."
            : $"Delete \"{symbol.Name}\"?";
        if (MessageBox.Show(msg, "Symbol Commander", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;

        _owner.Working.Bindings.RemoveAll(b => b.SymbolId == symbol.Id);
        _owner.Store.DeleteCustomSymbol(symbol.Id);
        Reload();
        // stop matching the deleted symbol immediately, mirroring New_Click
        _owner.Coordinator.ApplyConfig(_owner.Coordinator.CurrentConfig, _owner.Store.LoadCustomSymbols());
    }
}
