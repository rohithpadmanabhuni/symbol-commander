namespace SymbolCommander.Core.Recognition;

public sealed record RecognitionResult(string? SymbolId, double Score)
{
    public bool IsMatch => SymbolId is not null;
    public static readonly RecognitionResult None = new((string?)null, 0);
}
