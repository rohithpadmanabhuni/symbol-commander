using SymbolCommander.Core.Library;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class BuiltInSymbolsTests
{
    // Simulate a human drawing: scale to ~300px, offset, jitter ±4px (seeded), rotate 8°
    private static List<GesturePoint> Humanize(GesturePoint[] template, int seed)
    {
        var rng = new Random(seed);
        double rot = 8 * Math.PI / 180, cos = Math.Cos(rot), sin = Math.Sin(rot);
        return template.Select(p =>
        {
            double x = p.X * 300 + 100 + (rng.NextDouble() - 0.5) * 8;
            double y = p.Y * 300 + 100 + (rng.NextDouble() - 0.5) * 8;
            return new GesturePoint(
                150 + (x - 150) * cos - (y - 150) * sin,
                150 + (x - 150) * sin + (y - 150) * cos);
        }).ToList();
    }

    [Fact]
    public void Library_has_expected_symbols()
    {
        var ids = BuiltInSymbols.All.Select(s => s.Id).ToHashSet();
        var expected = new[] { "c", "l", "m", "n", "s", "u", "v", "w", "z",
            "circle", "triangle", "check", "caret", "up", "down", "left", "right" };
        Assert.Equal(expected.Length, ids.Count);
        Assert.All(expected, id => Assert.Contains(id, ids));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Every_symbol_recognizes_a_humanized_drawing_of_itself(int seed)
    {
        foreach (var sym in BuiltInSymbols.All)
        {
            var drawn = Humanize(sym.TemplateStrokes[0], seed * 31 + sym.Id.Length);
            var result = ProtractorRecognizer.Recognize(drawn, BuiltInSymbols.Templates, 0.80);
            Assert.True(result.IsMatch, $"{sym.Id} (seed {seed}) did not match anything, score {result.Score:F3}");
            Assert.True(sym.Id == result.SymbolId,
                $"{sym.Id} (seed {seed}) recognized as {result.SymbolId} ({result.Score:F3})");
        }
    }

    [Fact]
    public void M_and_W_are_distinct()
    {
        var m = BuiltInSymbols.All.First(s => s.Id == "m");
        var result = ProtractorRecognizer.Recognize(
            Humanize(m.TemplateStrokes[0], 7), BuiltInSymbols.Templates, 0.80);
        Assert.Equal("m", result.SymbolId);
    }

    [Fact]
    public void Counterclockwise_circle_also_recognizes_as_circle()
    {
        var circle = BuiltInSymbols.All.First(s => s.Id == "circle");
        Assert.True(circle.TemplateStrokes.Count >= 2);
        var drawn = Humanize(circle.TemplateStrokes[1], 11);
        var result = ProtractorRecognizer.Recognize(drawn, BuiltInSymbols.Templates, 0.80);
        Assert.Equal("circle", result.SymbolId);
    }

    [Fact]
    public void Every_template_stroke_has_at_least_32_points()
    {
        foreach (var sym in BuiltInSymbols.All)
            Assert.All(sym.TemplateStrokes, s => Assert.True(s.Length >= 32));
    }
}
