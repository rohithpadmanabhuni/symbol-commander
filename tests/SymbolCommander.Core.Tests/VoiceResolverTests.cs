using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Voice;

namespace SymbolCommander.Core.Tests;

public class VoiceResolverTests
{
    private static ActionDefinition Act(string name, string voice = "") =>
        new() { Name = name, Type = ActionType.Keystroke, Voice = voice };

    private static readonly string[] Pack =
        { "minimize.mp3", "maximize.mp3", "back.mp3", "open.mp3", "website.mp3",
          "web.mp3", "startup.mp3", "tts/volume-up.wav" };

    [Fact]
    public void Slug_lowercases_and_collapses_non_alphanumerics()
    {
        Assert.Equal("volume-up", VoiceResolver.Slug("Volume up"));
        Assert.Equal("ctrl-shift-t", VoiceResolver.Slug("  Ctrl+Shift+T! "));
        Assert.Equal("undo", VoiceResolver.Slug("Undo"));
    }

    [Fact]
    public void Auto_matches_clip_whose_base_name_is_a_word_of_the_action_name()
    {
        var r = VoiceResolver.Resolve(Act("Minimize window"), Pack);
        Assert.Equal(VoiceResolutionKind.File, r.Kind);
        Assert.Equal("minimize.mp3", r.FileName);
    }

    [Fact]
    public void Auto_tie_break_prefers_longest_base_name()
    {
        // "Open website" matches open, web, AND website — website (7) must win
        var r = VoiceResolver.Resolve(Act("Open the website"), Pack);
        Assert.Equal("website.mp3", r.FileName);
    }

    [Fact]
    public void Auto_falls_back_to_existing_tts_sample()
    {
        var r = VoiceResolver.Resolve(Act("Volume up"), Pack);
        Assert.Equal(VoiceResolutionKind.File, r.Kind);
        Assert.Equal("tts/volume-up.wav", r.FileName);
    }

    [Fact]
    public void Auto_requests_tts_when_nothing_matches()
    {
        var r = VoiceResolver.Resolve(Act("Undo"), Pack);
        Assert.Equal(VoiceResolutionKind.TtsNeeded, r.Kind);
        Assert.Equal("Undo", r.TtsText);
    }

    [Fact]
    public void Tts_files_never_match_as_clips()
    {
        // action named "volume-up" should not word-match the tts file's base name path
        var r = VoiceResolver.Resolve(Act("Something else"), new[] { "tts/something.wav" });
        Assert.Equal(VoiceResolutionKind.TtsNeeded, r.Kind);
    }

    [Fact]
    public void Explicit_file_wins_over_auto()
    {
        var r = VoiceResolver.Resolve(Act("Minimize window", voice: "back.mp3"), Pack);
        Assert.Equal("back.mp3", r.FileName);
    }

    [Fact]
    public void Explicit_file_is_case_insensitive()
    {
        var r = VoiceResolver.Resolve(Act("X", voice: "Back.MP3"), Pack);
        Assert.Equal("back.mp3", r.FileName);
    }

    [Fact]
    public void Missing_explicit_file_falls_back_to_auto()
    {
        var r = VoiceResolver.Resolve(Act("Minimize window", voice: "gone.mp3"), Pack);
        Assert.Equal("minimize.mp3", r.FileName);
    }

    [Fact]
    public void None_is_silent()
    {
        var r = VoiceResolver.Resolve(Act("Minimize window", voice: "none"), Pack);
        Assert.Equal(VoiceResolutionKind.None, r.Kind);
    }

    [Fact]
    public void Empty_file_list_requests_tts()
    {
        var r = VoiceResolver.Resolve(Act("Minimize window"), Array.Empty<string>());
        Assert.Equal(VoiceResolutionKind.TtsNeeded, r.Kind);
        Assert.Equal("Minimize window", r.TtsText);
    }
}
