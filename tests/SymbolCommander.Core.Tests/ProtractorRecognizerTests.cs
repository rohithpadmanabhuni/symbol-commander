using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class ProtractorRecognizerTests
{
    // dense "V" stroke: down-right then up-right, 300px scale
    private static List<GesturePoint> VStroke()
    {
        var pts = new List<GesturePoint>();
        for (int i = 0; i <= 30; i++) pts.Add(new GesturePoint(i * 5, i * 10));
        for (int i = 0; i <= 30; i++) pts.Add(new GesturePoint(150 + i * 5, 300 - i * 10));
        return pts;
    }

    // dense circle stroke, clockwise from top
    private static List<GesturePoint> CircleStroke()
    {
        var pts = new List<GesturePoint>();
        for (int i = 0; i <= 64; i++)
        {
            double th = Math.PI / 2 - 2 * Math.PI * i / 64.0;
            pts.Add(new GesturePoint(200 + 150 * Math.Cos(th), 200 - 150 * Math.Sin(th)));
        }
        return pts;
    }

    private static List<GesturePoint> Rotated(List<GesturePoint> pts, double degrees)
    {
        double cx = pts.Average(p => p.X), cy = pts.Average(p => p.Y);
        double r = degrees * Math.PI / 180, cos = Math.Cos(r), sin = Math.Sin(r);
        return pts.Select(p => new GesturePoint(
            cx + (p.X - cx) * cos - (p.Y - cy) * sin,
            cy + (p.X - cx) * sin + (p.Y - cy) * cos)).ToList();
    }

    private static SymbolTemplate T(string id, List<GesturePoint> pts) =>
        new(id, ProtractorRecognizer.ToVector(pts));

    [Fact]
    public void Identical_stroke_matches_with_near_perfect_score()
    {
        var result = ProtractorRecognizer.Recognize(VStroke(), new[] { T("v", VStroke()) }, 0.80);
        Assert.True(result.IsMatch);
        Assert.Equal("v", result.SymbolId);
        Assert.True(result.Score > 0.99, $"score was {result.Score}");
    }

    [Fact]
    public void Slightly_rotated_stroke_still_matches()
    {
        var result = ProtractorRecognizer.Recognize(
            Rotated(VStroke(), 20), new[] { T("v", VStroke()) }, 0.80);
        Assert.True(result.IsMatch);
        Assert.True(result.Score > 0.95, $"score was {result.Score}");
    }

    [Fact]
    public void Heavily_rotated_stroke_does_not_match()
    {
        // V rotated 180° is a caret — must NOT match V (this is the M-vs-W guarantee)
        var result = ProtractorRecognizer.Recognize(
            Rotated(VStroke(), 180), new[] { T("v", VStroke()) }, 0.80);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Best_of_multiple_templates_wins()
    {
        var templates = new[] { T("v", VStroke()), T("circle", CircleStroke()) };
        var result = ProtractorRecognizer.Recognize(CircleStroke(), templates, 0.80);
        Assert.Equal("circle", result.SymbolId);
    }

    [Fact]
    public void Different_shape_scores_below_threshold()
    {
        var result = ProtractorRecognizer.Recognize(VStroke(), new[] { T("circle", CircleStroke()) }, 0.80);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Invalid_stroke_returns_no_match_score_zero()
    {
        var tiny = new List<GesturePoint> { new(0, 0), new(1, 1), new(2, 2) };
        var result = ProtractorRecognizer.Recognize(tiny, new[] { T("v", VStroke()) }, 0.80);
        Assert.False(result.IsMatch);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Empty_template_list_returns_no_match()
    {
        var result = ProtractorRecognizer.Recognize(VStroke(), Array.Empty<SymbolTemplate>(), 0.80);
        Assert.False(result.IsMatch);
    }
}
