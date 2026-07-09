using System.IO.Compression;

namespace SymbolCommander.Core.Voice;

/// <summary>
/// Ingests a voice pack (zip or folder) into the voice directory: audio files only
/// (.mp3/.wav), subfolders flattened, "…Sensiva Sound…" renamed to startup.mp3,
/// existing files overwritten.
/// </summary>
public static class VoicePackImporter
{
    public static int ImportZip(string zipPath, string voiceDir)
    {
        Directory.CreateDirectory(voiceDir);
        try
        {
            using var zip = ZipFile.OpenRead(zipPath);
            int count = 0;
            foreach (var entry in zip.Entries)
            {
                if (!IsAudio(entry.Name)) continue;
                using var src = entry.Open(); // throws InvalidDataException on unsupported compression
                using var dst = File.Create(Path.Combine(voiceDir, TargetName(entry.Name)));
                src.CopyTo(dst);
                count++;
            }
            return count;
        }
        catch (InvalidDataException ex)
        {
            throw new NotSupportedException(
                "This zip uses a compression format .NET can't read (the original Sensiva pack " +
                "is LZMA-compressed). Extract it with Windows Explorer or 7-Zip, then use " +
                "\"Import folder…\" instead.", ex);
        }
    }

    public static int ImportFolder(string folderPath, string voiceDir)
    {
        Directory.CreateDirectory(voiceDir);
        int count = 0;
        foreach (var file in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(file);
            if (!IsAudio(name)) continue;
            File.Copy(file, Path.Combine(voiceDir, TargetName(name)), overwrite: true);
            count++;
        }
        return count;
    }

    private static bool IsAudio(string fileName) =>
        fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
        fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase);

    private static string TargetName(string fileName) =>
        fileName.Contains("sensiva sound", StringComparison.OrdinalIgnoreCase)
            ? "startup.mp3"
            : fileName;
}
