using System.IO.Compression;
using SymbolCommander.Core.Voice;

namespace SymbolCommander.Core.Tests;

public class VoicePackImporterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sc-voice-" + Guid.NewGuid().ToString("N"));
    private string VoiceDir => Path.Combine(_root, "voice");

    public VoicePackImporterTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private string MakeZip(params (string entry, string content)[] entries)
    {
        var zipPath = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".zip");
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (entry, content) in entries)
        {
            var e = zip.CreateEntry(entry, CompressionLevel.Optimal);
            using var w = new StreamWriter(e.Open());
            w.Write(content);
        }
        return zipPath;
    }

    [Fact]
    public void ImportZip_copies_audio_flattens_and_renames_sensiva_sound()
    {
        var zip = MakeZip(
            ("English Female Voice/minimize.mp3", "AUDIO-MIN"),
            ("English Female Voice/services/email.mp3", "AUDIO-EMAIL"),
            ("English Female Voice/The Sensiva Sound.mp3", "AUDIO-JINGLE"),
            ("English Female Voice/events", "not audio"),
            ("English Female Voice/readme.txt", "not audio"));

        int n = VoicePackImporter.ImportZip(zip, VoiceDir);

        Assert.Equal(3, n);
        Assert.Equal("AUDIO-MIN", File.ReadAllText(Path.Combine(VoiceDir, "minimize.mp3")));
        Assert.Equal("AUDIO-EMAIL", File.ReadAllText(Path.Combine(VoiceDir, "email.mp3")));
        Assert.Equal("AUDIO-JINGLE", File.ReadAllText(Path.Combine(VoiceDir, "startup.mp3")));
        Assert.False(File.Exists(Path.Combine(VoiceDir, "events")));
        Assert.False(File.Exists(Path.Combine(VoiceDir, "readme.txt")));
    }

    [Fact]
    public void ImportZip_overwrites_existing_files()
    {
        Directory.CreateDirectory(VoiceDir);
        File.WriteAllText(Path.Combine(VoiceDir, "minimize.mp3"), "OLD");
        var zip = MakeZip(("minimize.mp3", "NEW"));

        VoicePackImporter.ImportZip(zip, VoiceDir);

        Assert.Equal("NEW", File.ReadAllText(Path.Combine(VoiceDir, "minimize.mp3")));
    }

    [Fact]
    public void ImportZip_with_unsupported_compression_throws_friendly_error()
    {
        // build a valid deflate zip, then lie about the compression method (14 = LZMA)
        // in both the local file header (PK\x03\x04, offset +8) and the central
        // directory header (PK\x01\x02, offset +10)
        var zip = MakeZip(("clip.mp3", new string('x', 2000))); // compressible → deflate
        var bytes = File.ReadAllBytes(zip);
        for (int i = 0; i + 11 < bytes.Length; i++)
        {
            if (bytes[i] != 0x50 || bytes[i + 1] != 0x4B) continue;
            if (bytes[i + 2] == 0x03 && bytes[i + 3] == 0x04) { bytes[i + 8] = 14; bytes[i + 9] = 0; }
            else if (bytes[i + 2] == 0x01 && bytes[i + 3] == 0x02) { bytes[i + 10] = 14; bytes[i + 11] = 0; }
        }
        File.WriteAllBytes(zip, bytes);

        var ex = Assert.Throws<NotSupportedException>(() => VoicePackImporter.ImportZip(zip, VoiceDir));
        Assert.Contains("Import folder", ex.Message);
    }

    [Fact]
    public void ImportFolder_copies_recursively_with_same_rules()
    {
        var src = Path.Combine(_root, "pack");
        Directory.CreateDirectory(Path.Combine(src, "services"));
        File.WriteAllText(Path.Combine(src, "back.mp3"), "AUDIO-BACK");
        File.WriteAllText(Path.Combine(src, "custom.wav"), "AUDIO-WAV");
        File.WriteAllText(Path.Combine(src, "services", "news.mp3"), "AUDIO-NEWS");
        File.WriteAllText(Path.Combine(src, "The Sensiva Sound.mp3"), "AUDIO-JINGLE");
        File.WriteAllText(Path.Combine(src, "events"), "not audio");

        int n = VoicePackImporter.ImportFolder(src, VoiceDir);

        Assert.Equal(4, n);
        Assert.True(File.Exists(Path.Combine(VoiceDir, "back.mp3")));
        Assert.True(File.Exists(Path.Combine(VoiceDir, "custom.wav")));
        Assert.True(File.Exists(Path.Combine(VoiceDir, "news.mp3")));
        Assert.True(File.Exists(Path.Combine(VoiceDir, "startup.mp3")));
        Assert.False(File.Exists(Path.Combine(VoiceDir, "events")));
    }

    [Fact]
    public void Import_creates_voice_dir_when_missing()
    {
        Assert.False(Directory.Exists(VoiceDir));
        var zip = MakeZip(("a.mp3", "A"));
        VoicePackImporter.ImportZip(zip, VoiceDir);
        Assert.True(Directory.Exists(VoiceDir));
    }
}
