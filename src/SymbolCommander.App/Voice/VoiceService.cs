using System.IO;
using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Voice;

namespace SymbolCommander.App.Voice;

/// <summary>
/// Façade over the voice directory: file listing, resolution, playback, TTS generation.
/// Construct and call playback methods on the UI thread; TTS generation for uncached
/// actions runs on a background thread and marshals back to play.
/// </summary>
public sealed class VoiceService
{
    private readonly Action<string, string, bool> _notify; // title, message, warning
    private readonly VoicePlayer _player = new();
    private bool _ttsFailedThisSession;

    public string VoiceDir { get; }
    private string TtsDir => Path.Combine(VoiceDir, "tts");

    public VoiceService(string voiceDir, Action<string, string, bool> notify)
    {
        VoiceDir = voiceDir;
        _notify = notify;
    }

    public IReadOnlyList<string> AvailableFiles()
    {
        if (!Directory.Exists(VoiceDir)) return Array.Empty<string>();
        return Directory.EnumerateFiles(VoiceDir, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetRelativePath(VoiceDir, f).Replace('\\', '/'))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void PlayForAction(ActionDefinition action, double volume)
    {
        var resolution = VoiceResolver.Resolve(action, AvailableFiles());
        switch (resolution.Kind)
        {
            case VoiceResolutionKind.File:
                PlayFile(resolution.FileName!, volume);
                break;
            case VoiceResolutionKind.TtsNeeded:
                Task.Run(() =>
                {
                    var rel = TryGenerateTts(resolution.TtsText!);
                    if (rel is null) return;
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                        () => PlayFile(rel, volume));
                });
                break;
        }
    }

    public void PlayStartup(double volume)
    {
        var startup = AvailableFiles().FirstOrDefault(f =>
            !f.Contains('/') &&
            Path.GetFileNameWithoutExtension(f).Equals("startup", StringComparison.OrdinalIgnoreCase));
        if (startup is not null) PlayFile(startup, volume);
    }

    public void PlayFile(string relativeName, double volume) =>
        _player.Play(Path.Combine(VoiceDir, relativeName), volume);

    public string? TryGenerateTts(string text)
    {
        try
        {
            Directory.CreateDirectory(TtsDir);
            var rel = "tts/" + VoiceResolver.Slug(text) + ".wav";
            TtsSampleGenerator.RenderToFile(text, Path.Combine(VoiceDir, rel));
            return rel;
        }
        catch (Exception ex)
        {
            if (!_ttsFailedThisSession)
            {
                _ttsFailedThisSession = true;
                _notify("Voice", $"Text-to-speech unavailable: {ex.Message}", true);
            }
            return null;
        }
    }
}
