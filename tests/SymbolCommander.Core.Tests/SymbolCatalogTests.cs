using SymbolCommander.Core.Config;
using SymbolCommander.Core.Library;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class SymbolCatalogTests
{
    private static CustomSymbol Star()
    {
        var pts = new List<GesturePoint>();
        for (int i = 0; i <= 40; i++) pts.Add(new GesturePoint(i * 5, (i % 2) * 100));
        return new CustomSymbol { Name = "Star", Examples = { pts, pts, pts } };
    }

    [Fact]
    public void Catalog_merges_built_ins_and_customs()
    {
        var star = Star();
        var catalog = new SymbolCatalog(new[] { star });
        Assert.Equal(BuiltInSymbols.All.Count + 1, catalog.All.Count);
        Assert.Equal("Star", catalog.NameOf(star.Id));
        Assert.Equal("M", catalog.NameOf("m"));
        Assert.Null(catalog.NameOf("nonexistent"));
        Assert.Contains(catalog.All, e => e.Id == star.Id && !e.IsBuiltIn);
    }

    [Fact]
    public void Custom_symbol_contributes_one_template_per_example()
    {
        var star = Star();
        var catalog = new SymbolCatalog(new[] { star });
        Assert.Equal(3, catalog.AllTemplates.Count(t => t.SymbolId == star.Id));
    }

    [Fact]
    public void TemplatesFor_filters_by_symbol_id()
    {
        var catalog = new SymbolCatalog(Array.Empty<CustomSymbol>());
        var templates = catalog.TemplatesFor(new[] { "m", "circle" });
        Assert.All(templates, t => Assert.True(t.SymbolId is "m" or "circle"));
        Assert.Equal(3, templates.Count); // m has 1 template, circle has 2
    }
}
