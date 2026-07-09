namespace SymbolCommander.Core.Recognition;

/// <summary>
/// Protractor unistroke matcher (Li, CHI 2010): closed-form optimal-rotation cosine
/// similarity between magnitude-normalized point vectors. Orientation-sensitive:
/// the optimal rotation is clamped to ±30° so e.g. M and W (180° apart) stay distinct.
/// </summary>
public static class ProtractorRecognizer
{
    public const double MaxRotationRadians = Math.PI / 6;

    public static double[] ToVector(IReadOnlyList<GesturePoint> rawStroke) =>
        StrokePreprocessor.Vectorize(StrokePreprocessor.Normalize(StrokePreprocessor.Resample(rawStroke)));

    public static double Similarity(double[] template, double[] candidate)
    {
        double a = 0, b = 0;
        int len = Math.Min(template.Length, candidate.Length);
        for (int i = 0; i + 1 < len; i += 2)
        {
            a += template[i] * candidate[i] + template[i + 1] * candidate[i + 1];
            b += template[i] * candidate[i + 1] - template[i + 1] * candidate[i];
        }
        double angle = Math.Clamp(Math.Atan2(b, a), -MaxRotationRadians, MaxRotationRadians);
        return Math.Min(1.0, a * Math.Cos(angle) + b * Math.Sin(angle));
    }

    public static RecognitionResult Recognize(
        IReadOnlyList<GesturePoint> raw, IEnumerable<SymbolTemplate> templates, double threshold)
    {
        if (!StrokePreprocessor.IsValidStroke(raw)) return RecognitionResult.None;
        var g = ToVector(raw);
        string? bestId = null;
        double bestScore = double.MinValue;
        foreach (var t in templates)
        {
            double s = Similarity(t.Vector, g);
            if (s > bestScore) { bestScore = s; bestId = t.SymbolId; }
        }
        if (bestId is null) return RecognitionResult.None;
        return bestScore >= threshold
            ? new RecognitionResult(bestId, bestScore)
            : new RecognitionResult(null, Math.Max(0, bestScore));
    }
}
