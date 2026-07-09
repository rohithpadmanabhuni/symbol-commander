using SymbolCommander.Core.Library;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Config;

/// <summary>Immutable merged view of built-in and custom symbols. Rebuild after config changes.</summary>
public sealed class SymbolCatalog
{
    private readonly Dictionary<string, string> _names = new();

    public IReadOnlyList<(string Id, string Name, bool IsBuiltIn)> All { get; }
    public IReadOnlyList<SymbolTemplate> AllTemplates { get; }

    public SymbolCatalog(IEnumerable<CustomSymbol> customs)
    {
        var all = new List<(string, string, bool)>();
        var templates = new List<SymbolTemplate>(BuiltInSymbols.Templates);

        foreach (var s in BuiltInSymbols.All)
        {
            all.Add((s.Id, s.Name, true));
            _names[s.Id] = s.Name;
        }
        foreach (var c in customs)
        {
            all.Add((c.Id, c.Name, false));
            _names[c.Id] = c.Name;
            templates.AddRange(c.Examples.Select(e =>
                new SymbolTemplate(c.Id, ProtractorRecognizer.ToVector(e))));
        }
        All = all;
        AllTemplates = templates;
    }

    public string? NameOf(string symbolId) => _names.TryGetValue(symbolId, out var n) ? n : null;

    public IReadOnlyList<SymbolTemplate> TemplatesFor(IEnumerable<string> symbolIds)
    {
        var wanted = symbolIds.ToHashSet();
        return AllTemplates.Where(t => wanted.Contains(t.SymbolId)).ToList();
    }
}
