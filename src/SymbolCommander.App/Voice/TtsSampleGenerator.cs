using System.Speech.Synthesis;

namespace SymbolCommander.App.Voice;

/// <summary>Renders text to a WAV file with the default installed SAPI voice.
/// Synchronous — call from a background thread.</summary>
public static class TtsSampleGenerator
{
    public static void RenderToFile(string text, string wavPath)
    {
        using var synth = new SpeechSynthesizer();
        synth.SetOutputToWaveFile(wavPath);
        synth.Speak(text);
    }
}
