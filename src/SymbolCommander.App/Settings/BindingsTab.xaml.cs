using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Recognition;
// disambiguate from System.Windows.Forms.Binding (WinForms interop is enabled)
using Binding = SymbolCommander.Core.Actions.Binding;

namespace SymbolCommander.App.Settings;

public partial class BindingsTab : UserControl
{
    public sealed record Choice(string Id, string Name);

    public sealed class BindingRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public IReadOnlyList<Choice> Symbols { get; init; } = Array.Empty<Choice>();
        public IReadOnlyList<Choice> Actions { get; init; } = Array.Empty<Choice>();

        private string _symbolId = "", _actionId = "";
        private bool _enabled = true;

        public string SymbolId { get => _symbolId; set { _symbolId = value; Notify(nameof(SymbolId)); } }
        public string ActionId { get => _actionId; set { _actionId = value; Notify(nameof(ActionId)); } }
        public bool Enabled { get => _enabled; set { _enabled = value; Notify(nameof(Enabled)); } }
        private void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    private SettingsWindow _owner = null!;
    private readonly ObservableCollection<BindingRow> _rows = new();
    private SymbolCatalog _catalog = new(Array.Empty<CustomSymbol>());
    private bool _loaded;

    public BindingsTab()
    {
        InitializeComponent();
        BindingsGrid.ItemsSource = _rows;
        TestCanvas.StrokeDrawn += OnTestStroke;
    }

    public void Load(SettingsWindow owner)
    {
        _owner = owner;
        Reload();
        _loaded = true;
    }

    public void Reload()
    {
        // keep unsaved row edits when the user tabs away and back
        if (_loaded) CollectInto(_owner.Working);
        _catalog = new SymbolCatalog(_owner.Store.LoadCustomSymbols());
        var symbols = _catalog.All.Select(s => new Choice(s.Id, s.IsBuiltIn ? s.Name : $"{s.Name} (custom)")).ToList();
        var actions = _owner.Working.Actions.Select(a => new Choice(a.Id, a.Name)).ToList();
        _rows.Clear();
        foreach (var b in _owner.Working.Bindings)
            _rows.Add(new BindingRow { Symbols = symbols, Actions = actions,
                SymbolId = b.SymbolId, ActionId = b.ActionId, Enabled = b.Enabled });
    }

    public void CollectInto(AppConfig working)
    {
        working.Bindings.Clear();
        foreach (var r in _rows.Where(r => r.SymbolId != "" && r.ActionId != ""))
            working.Bindings.Add(new Binding { SymbolId = r.SymbolId, ActionId = r.ActionId, Enabled = r.Enabled });
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var symbols = _catalog.All.Select(s => new Choice(s.Id, s.IsBuiltIn ? s.Name : $"{s.Name} (custom)")).ToList();
        var actions = _owner.Working.Actions.Select(a => new Choice(a.Id, a.Name)).ToList();
        _rows.Add(new BindingRow { Symbols = symbols, Actions = actions });
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (BindingsGrid.SelectedItem is BindingRow row) _rows.Remove(row);
    }

    private void OnTestStroke(IReadOnlyList<GesturePoint> stroke)
    {
        var result = ProtractorRecognizer.Recognize(
            stroke, _catalog.AllTemplates, _owner.Working.Settings.Sensitivity);
        TestResult.Text = result.IsMatch
            ? $"Recognized: {_catalog.NameOf(result.SymbolId!)}  (score {result.Score:0.00})"
            : $"No match  (best score {result.Score:0.00})";
    }
}
