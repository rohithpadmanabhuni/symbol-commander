using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Library;

public sealed record BuiltInSymbol(string Id, string Name, IReadOnlyList<GesturePoint[]> TemplateStrokes);

public static class BuiltInSymbols
{
    public static IReadOnlyList<BuiltInSymbol> All { get; }
    public static IReadOnlyList<SymbolTemplate> Templates { get; }

    static BuiltInSymbols()
    {
        static GesturePoint[] Poly(params (double x, double y)[] v) =>
            StrokePreprocessor.Resample(v.Select(p => new GesturePoint(p.x, p.y)).ToList(), 64);

        // point(θ): screen coords, θ=90° points visually up (y decreases)
        static GesturePoint[] Arc(double cx, double cy, double r, double startDeg, double endDeg)
        {
            var pts = new List<GesturePoint>();
            for (int i = 0; i <= 63; i++)
            {
                double th = (startDeg + (endDeg - startDeg) * i / 63.0) * Math.PI / 180;
                pts.Add(new GesturePoint(cx + r * Math.Cos(th), cy - r * Math.Sin(th)));
            }
            return pts.ToArray();
        }

        All = new List<BuiltInSymbol>
        {
            new("c", "C", new[] { Arc(0.5, 0.5, 0.45, 60, 300) }),
            new("l", "L", new[] { Poly((0, 0), (0, 1), (1, 1)) }),
            new("m", "M", new[] { Poly((0, 1), (0, 0), (0.5, 0.6), (1, 0), (1, 1)) }),
            new("n", "N", new[] { Poly((0, 1), (0, 0), (1, 1), (1, 0)) }),
            new("s", "S", new[] { Poly((0.75, 0.05), (0.4, 0.05), (0.25, 0.2), (0.35, 0.42),
                                       (0.65, 0.58), (0.75, 0.8), (0.6, 0.95), (0.25, 0.95)) }),
            new("u", "U", new[] { Poly((0, 0), (0, 0.6), (0.15, 0.9), (0.5, 1),
                                       (0.85, 0.9), (1, 0.6), (1, 0)) }),
            new("v", "V", new[] { Poly((0, 0), (0.5, 1), (1, 0)) }),
            new("w", "W", new[] { Poly((0, 0), (0.25, 1), (0.5, 0.4), (0.75, 1), (1, 0)) }),
            new("z", "Z", new[] { Poly((0, 0), (1, 0), (0, 1), (1, 1)) }),
            new("circle", "Circle", new[]
            {
                Arc(0.5, 0.5, 0.45, 90, -270),  // clockwise from top
                Arc(0.5, 0.5, 0.45, 90, 450),   // counterclockwise from top
            }),
            new("triangle", "Triangle", new[] { Poly((0.5, 0), (0, 1), (1, 1), (0.5, 0)) }),
            new("check", "Check", new[] { Poly((0, 0.55), (0.35, 1), (1, 0)) }),
            new("caret", "Caret", new[] { Poly((0, 1), (0.5, 0), (1, 1)) }),
            new("up", "Up", new[] { Poly((0.5, 1), (0.5, 0)) }),
            new("down", "Down", new[] { Poly((0.5, 0), (0.5, 1)) }),
            new("left", "Left", new[] { Poly((1, 0.5), (0, 0.5)) }),
            new("right", "Right", new[] { Poly((0, 0.5), (1, 0.5)) }),
        };

        Templates = All
            .SelectMany(s => s.TemplateStrokes.Select(st =>
                new SymbolTemplate(s.Id, ProtractorRecognizer.ToVector(st))))
            .ToList();
    }
}
