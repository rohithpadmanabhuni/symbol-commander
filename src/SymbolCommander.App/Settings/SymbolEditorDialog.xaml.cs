using System.Windows;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.App.Settings;

public partial class SymbolEditorDialog : Window
{
    private const int MinExamples = 3;
    private const int MaxExamples = 5;
    private const double CollisionThreshold = 0.85;

    private readonly ConfigStore _store;
    private readonly SymbolCatalog _existing;
    private readonly List<List<GesturePoint>> _examples = new();

    public CustomSymbol? Result { get; private set; }

    public SymbolEditorDialog(ConfigStore store)
    {
        InitializeComponent();
        _store = store;
        _existing = new SymbolCatalog(store.LoadCustomSymbols());
        Canvas.StrokeDrawn += OnStroke;
        UpdateStatus();
    }

    private void OnStroke(IReadOnlyList<GesturePoint> stroke)
    {
        if (!StrokePreprocessor.IsValidStroke(stroke))
        {
            StatusLabel.Text = "Stroke too small — draw bigger.";
            return;
        }
        if (_examples.Count >= MaxExamples)
        {
            StatusLabel.Text = $"Already have {MaxExamples} examples. Save, or clear and redraw.";
            return;
        }
        _examples.Add(stroke.ToList());
        UpdateStatus();
    }

    private double Consistency()
    {
        var vectors = _examples.Select(ProtractorRecognizer.ToVector).ToList();
        var scores = new List<double>();
        for (int i = 0; i < vectors.Count; i++)
            for (int j = i + 1; j < vectors.Count; j++)
                scores.Add(ProtractorRecognizer.Similarity(vectors[i], vectors[j]));
        return scores.Count == 0 ? 1 : scores.Average();
    }

    private void UpdateStatus()
    {
        string text = $"Examples: {_examples.Count}/{MaxExamples}";
        if (_examples.Count >= 2)
        {
            double c = Consistency();
            text += $"   Consistency: {c:P0} " + c switch
            {
                >= 0.85 => "— good",
                >= 0.70 => "— okay, try to draw more alike",
                _ => "— inconsistent, clear and redraw",
            };
        }
        StatusLabel.Text = text;
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _examples.Clear();
        Canvas.ClearInk();
        UpdateStatus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorLabel.Visibility = Visibility.Collapsed;
        var name = NameBox.Text.Trim();
        if (name.Length == 0) { ShowError("Give the symbol a name."); return; }
        if (_existing.All.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        { ShowError($"A symbol named \"{name}\" already exists."); return; }
        if (_examples.Count < MinExamples)
        { ShowError($"Draw at least {MinExamples} examples ({_examples.Count} so far)."); return; }

        // collision check: does any example match an existing symbol too closely?
        double worst = 0; string? collidesWith = null;
        foreach (var vec in _examples.Select(ProtractorRecognizer.ToVector))
            foreach (var t in _existing.AllTemplates)
            {
                double s = ProtractorRecognizer.Similarity(t.Vector, vec);
                if (s > worst) { worst = s; collidesWith = _existing.NameOf(t.SymbolId); }
            }
        if (worst >= CollisionThreshold)
        {
            var choice = MessageBox.Show(
                $"This looks very similar to \"{collidesWith}\" (similarity {worst:P0}). " +
                "The two may steal each other's matches. Save anyway?",
                "Symbol Commander", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (choice != MessageBoxResult.Yes) return;
        }

        var symbol = new CustomSymbol { Name = name, Examples = _examples.ToList() };
        _store.SaveCustomSymbol(symbol);
        Result = symbol;
        DialogResult = true;
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.Visibility = Visibility.Visible;
    }
}
