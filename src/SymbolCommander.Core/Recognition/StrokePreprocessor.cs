namespace SymbolCommander.Core.Recognition;

public static class StrokePreprocessor
{
    public const int ResampleCount = 64;
    public const double MinPathLength = 30.0;
    public const int MinRawPoints = 5;

    public static double Distance(GesturePoint a, GesturePoint b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static double PathLength(IReadOnlyList<GesturePoint> pts)
    {
        double d = 0;
        for (int i = 1; i < pts.Count; i++) d += Distance(pts[i - 1], pts[i]);
        return d;
    }

    public static bool IsValidStroke(IReadOnlyList<GesturePoint> raw) =>
        raw.Count >= MinRawPoints && PathLength(raw) >= MinPathLength;

    public static GesturePoint[] Resample(IReadOnlyList<GesturePoint> pts, int n = ResampleCount)
    {
        // drop consecutive duplicates so zero-length segments can't divide by zero
        var src = new List<GesturePoint> { pts[0] };
        for (int i = 1; i < pts.Count; i++)
            if (pts[i] != src[^1]) src.Add(pts[i]);
        if (src.Count == 1) return Enumerable.Repeat(src[0], n).ToArray();

        double interval = PathLength(src) / (n - 1);
        var result = new List<GesturePoint>(n) { src[0] };
        double d = 0;
        for (int i = 1; i < src.Count; i++)
        {
            double seg = Distance(src[i - 1], src[i]);
            if (d + seg >= interval)
            {
                double t = (interval - d) / seg;
                var q = new GesturePoint(
                    src[i - 1].X + t * (src[i].X - src[i - 1].X),
                    src[i - 1].Y + t * (src[i].Y - src[i - 1].Y));
                result.Add(q);
                src.Insert(i, q);
                d = 0;
            }
            else d += seg;
        }
        while (result.Count < n) result.Add(src[^1]);
        return result.Take(n).ToArray();
    }

    public static GesturePoint[] Normalize(IReadOnlyList<GesturePoint> pts)
    {
        double minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X);
        double minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
        double scale = Math.Max(maxX - minX, maxY - minY);
        if (scale < 1e-9) scale = 1;
        double cx = pts.Average(p => p.X), cy = pts.Average(p => p.Y);
        return pts.Select(p => new GesturePoint((p.X - cx) / scale, (p.Y - cy) / scale)).ToArray();
    }

    public static double[] Vectorize(IReadOnlyList<GesturePoint> normalized)
    {
        var v = new double[normalized.Count * 2];
        for (int i = 0; i < normalized.Count; i++)
        {
            v[i * 2] = normalized[i].X;
            v[i * 2 + 1] = normalized[i].Y;
        }
        double mag = Math.Sqrt(v.Sum(x => x * x));
        if (mag < 1e-12) return v;
        for (int i = 0; i < v.Length; i++) v[i] /= mag;
        return v;
    }
}
