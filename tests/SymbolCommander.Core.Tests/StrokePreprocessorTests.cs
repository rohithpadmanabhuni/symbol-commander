using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class StrokePreprocessorTests
{
    private static List<GesturePoint> Line(double x0, double y0, double x1, double y1, int n)
    {
        var pts = new List<GesturePoint>();
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)(n - 1);
            pts.Add(new GesturePoint(x0 + t * (x1 - x0), y0 + t * (y1 - y0)));
        }
        return pts;
    }

    [Fact]
    public void PathLength_of_straight_line_is_its_length()
    {
        var pts = Line(0, 0, 300, 0, 10);
        Assert.Equal(300.0, StrokePreprocessor.PathLength(pts), 6);
    }

    [Fact]
    public void IsValidStroke_rejects_too_few_points()
    {
        Assert.False(StrokePreprocessor.IsValidStroke(Line(0, 0, 300, 0, 4)));
    }

    [Fact]
    public void IsValidStroke_rejects_too_short_path()
    {
        Assert.False(StrokePreprocessor.IsValidStroke(Line(0, 0, 20, 0, 10)));
    }

    [Fact]
    public void IsValidStroke_accepts_long_enough_stroke()
    {
        Assert.True(StrokePreprocessor.IsValidStroke(Line(0, 0, 300, 0, 10)));
    }

    [Fact]
    public void Resample_returns_exactly_n_evenly_spaced_points()
    {
        var resampled = StrokePreprocessor.Resample(Line(0, 0, 630, 0, 7), 64);
        Assert.Equal(64, resampled.Length);
        double expectedGap = 630.0 / 63;
        for (int i = 1; i < resampled.Length; i++)
        {
            double gap = StrokePreprocessor.Distance(resampled[i - 1], resampled[i]);
            Assert.InRange(gap, expectedGap - 0.5, expectedGap + 0.5);
        }
        Assert.Equal(0, resampled[0].X, 6);
        Assert.Equal(630, resampled[^1].X, 3);
    }

    [Fact]
    public void Resample_survives_consecutive_duplicate_points()
    {
        var pts = Line(0, 0, 100, 0, 10);
        pts.Insert(5, pts[4]); // exact duplicate
        pts.Insert(5, pts[4]);
        var resampled = StrokePreprocessor.Resample(pts, 32);
        Assert.Equal(32, resampled.Length);
        Assert.All(resampled, p => Assert.False(double.IsNaN(p.X) || double.IsNaN(p.Y)));
    }

    [Fact]
    public void Normalize_centers_centroid_at_origin_and_scales_max_dimension_to_one()
    {
        var pts = StrokePreprocessor.Resample(Line(100, 200, 500, 300, 20), 64);
        var norm = StrokePreprocessor.Normalize(pts);
        double cx = norm.Average(p => p.X), cy = norm.Average(p => p.Y);
        Assert.Equal(0, cx, 6);
        Assert.Equal(0, cy, 6);
        double w = norm.Max(p => p.X) - norm.Min(p => p.X);
        double h = norm.Max(p => p.Y) - norm.Min(p => p.Y);
        Assert.Equal(1.0, Math.Max(w, h), 6);
        // aspect preserved: original was 400 wide x 100 tall
        Assert.Equal(0.25, Math.Min(w, h) / Math.Max(w, h), 2);
    }

    [Fact]
    public void Normalize_handles_degenerate_zero_size_stroke_without_NaN()
    {
        var pts = Enumerable.Repeat(new GesturePoint(50, 50), 10).ToList();
        var norm = StrokePreprocessor.Normalize(pts);
        Assert.All(norm, p => Assert.False(double.IsNaN(p.X) || double.IsNaN(p.Y)));
    }

    [Fact]
    public void Vectorize_produces_unit_magnitude_vector_of_2n_elements()
    {
        var norm = StrokePreprocessor.Normalize(StrokePreprocessor.Resample(Line(0, 0, 300, 150, 20), 64));
        var v = StrokePreprocessor.Vectorize(norm);
        Assert.Equal(128, v.Length);
        double mag = Math.Sqrt(v.Sum(x => x * x));
        Assert.Equal(1.0, mag, 6);
    }
}
