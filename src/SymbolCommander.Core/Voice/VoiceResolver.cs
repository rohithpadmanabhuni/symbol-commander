using System.Text;
using SymbolCommander.Core.Actions;

namespace SymbolCommander.Core.Voice;

public enum VoiceResolutionKind { None, File, TtsNeeded }

public sealed record VoiceResolution(VoiceResolutionKind Kind, string? FileName = null, string? TtsText = null)
{
    public static readonly VoiceResolution None = new(VoiceResolutionKind.None);
}

/// <summary>
/// Decides what an action should sound like. Pure: takes the action and the list of
/// available voice files (voice-dir-relative, forward slashes) and returns a decision.
/// Auto order: word-matched clip → existing tts/&lt;slug&gt;.wav → TtsNeeded.
/// </summary>
public static class VoiceResolver
{
    public const string NoneValue = "none";

    public static string Slug(string name)
    {
        var sb = new StringBuilder();
        bool pendingHyphen = false;
        foreach (var c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                if (pendingHyphen && sb.Length > 0) sb.Append('-');
                pendingHyphen = false;
                sb.Append(c);
            }
            else pendingHyphen = true;
        }
        return sb.ToString();
    }

    public static VoiceResolution Resolve(ActionDefinition action, IReadOnlyCollection<string> availableFiles)
    {
        var voice = action.Voice ?? "";
        if (voice.Equals(NoneValue, StringComparison.OrdinalIgnoreCase))
            return VoiceResolution.None;

        if (voice.Length > 0)
        {
            var explicitFile = availableFiles.FirstOrDefault(
                f => f.Equals(voice, StringComparison.OrdinalIgnoreCase));
            if (explicitFile is not null)
                return new VoiceResolution(VoiceResolutionKind.File, explicitFile);
            // assigned file has gone missing — degrade predictably to Auto
        }

        return ResolveAuto(action.Name, availableFiles);
    }

    private static VoiceResolution ResolveAuto(string actionName, IReadOnlyCollection<string> files)
    {
        var words = Slug(actionName).Split('-', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        string? best = null;
        int bestLen = 0;
        foreach (var f in files)
        {
            if (f.Contains('/')) continue; // clips live in the voice root; tts/ etc. excluded
            var dot = f.LastIndexOf('.');
            var baseName = (dot > 0 ? f[..dot] : f).ToLowerInvariant();
            if (words.Contains(baseName) && baseName.Length > bestLen)
            {
                best = f;
                bestLen = baseName.Length;
            }
        }
        if (best is not null) return new VoiceResolution(VoiceResolutionKind.File, best);

        var tts = "tts/" + Slug(actionName) + ".wav";
        var ttsFile = files.FirstOrDefault(f => f.Equals(tts, StringComparison.OrdinalIgnoreCase));
        if (ttsFile is not null) return new VoiceResolution(VoiceResolutionKind.File, ttsFile);

        return new VoiceResolution(VoiceResolutionKind.TtsNeeded, TtsText: actionName);
    }
}
