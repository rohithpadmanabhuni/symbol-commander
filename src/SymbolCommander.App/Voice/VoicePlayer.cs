using System.Windows.Media;

namespace SymbolCommander.App.Voice;

/// <summary>Single MediaPlayer (MP3+WAV) with latest-wins playback. Create and call on
/// the UI thread. Voice is decoration: every failure path is swallowed.</summary>
public sealed class VoicePlayer
{
    private readonly MediaPlayer _player = new();

    public VoicePlayer()
    {
        _player.MediaOpened += (_, _) => _player.Play();
        _player.MediaFailed += (_, _) => { }; // corrupt/missing media must never surface
    }

    public void Play(string absolutePath, double volume)
    {
        try
        {
            _player.Stop();
            _player.Volume = Math.Clamp(volume, 0, 1);
            _player.Open(new Uri(absolutePath));
        }
        catch
        {
            // bad path/URI — skip silently
        }
    }
}
