# Voice Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Sensiva-style voice feedback — actions speak when they fire (original clip pack + TTS-generated samples), with global enable/volume controls and per-action sound management.

**Architecture:** Core gains a pure `VoiceResolver` (action → clip/TTS decision) and `VoicePackImporter` (zip/folder → voice dir), both fully unit-tested on Linux. The App gains a `VoiceService` (file listing + playback via WPF `MediaPlayer` + TTS sample generation via `System.Speech`), wired into the coordinator after action execution, plus UI: a Voice group in settings, a tray toggle, and a Voice row in the action editor.

**Tech Stack:** existing C#/.NET 8 solution; WPF `MediaPlayer` for playback (MP3+WAV, no new dependency); `System.Speech` NuGet package (Microsoft) for TTS — the one sanctioned exception to the zero-dependency rule.

**Spec:** `docs/superpowers/specs/2026-07-09-voice-feedback-design.md` (approved).

## Global Constraints

- All v1 global constraints still apply (`docs/superpowers/plans/2026-07-07-symbol-commander.md`), with ONE amendment: the App project may reference the `System.Speech` NuGet package. Core stays zero-dependency and Windows-free.
- Every shell command assumes: `export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"` and repo root `/home/dev1/Rohith/symbol-commander`.
- Run tests with `dotnet test tests/SymbolCommander.Core.Tests` (solution-level `dotnet test` silently discovers nothing — known quirk).
- Voice file names are voice-dir-relative with forward slashes (`minimize.mp3`, `tts/volume-up.wav`) everywhere in Core and config.
- `ActionDefinition.Voice` values: `""` = Auto, `"none"` = silent, else a relative file name.
- Voice dir on Windows: `%APPDATA%\SymbolCommander\voice`, TTS subdir `voice/tts`. Startup jingle = any root-level file with base name `startup`.
- Work on a feature branch `feature/voice` off `master`; commit per task.
- App tasks are compile-verified on Linux only; runtime behavior verifies via the Task 7 Windows checklist. Do not claim runtime success before that.

## File structure

```
src/SymbolCommander.Core/
├── Actions/ActionDefinition.cs        MODIFY Task 1 — add Voice property
├── Config/AppSettings.cs              MODIFY Task 1 — VoiceEnabled, VoiceVolume
├── Config/ConfigStore.cs              MODIFY Task 1 — expose Directory
└── Voice/
    ├── VoiceResolver.cs               CREATE Task 2 — Slug + Resolve
    └── VoicePackImporter.cs           CREATE Task 3 — ImportZip/ImportFolder
src/SymbolCommander.App/
├── SymbolCommander.App.csproj         MODIFY Task 4 — System.Speech package
├── Voice/
│   ├── VoicePlayer.cs                 CREATE Task 4 — MediaPlayer wrapper
│   ├── TtsSampleGenerator.cs          CREATE Task 4 — text → WAV
│   └── VoiceService.cs                CREATE Task 4 — files/play/tts façade
├── Engine/GestureCoordinator.cs       MODIFY Task 5 — play after execute, jingle, SetVoiceEnabled
├── Tray/TrayIcon.cs                   MODIFY Task 5 — Voice enabled item
├── App.xaml.cs                        MODIFY Task 5 — wire tray voice toggle
└── Settings/
    ├── GeneralTab.xaml(.cs)           MODIFY Task 6 — Voice group
    └── ActionEditorDialog.xaml(.cs)   MODIFY Task 6 — Voice row
tests/SymbolCommander.Core.Tests/
├── ConfigStoreTests.cs                MODIFY Task 1 — round-trip + back-compat
├── VoiceResolverTests.cs              CREATE Task 2
└── VoicePackImporterTests.cs          CREATE Task 3
README.md                              MODIFY Task 7 — Voice section
```

---

### Task 0: Branch

- [ ] **Step 1:**

```bash
cd /home/dev1/Rohith/symbol-commander && git checkout -b feature/voice
```

---

### Task 1: Core model + config additions

**Files:**
- Modify: `src/SymbolCommander.Core/Actions/ActionDefinition.cs`
- Modify: `src/SymbolCommander.Core/Config/AppSettings.cs`
- Modify: `src/SymbolCommander.Core/Config/ConfigStore.cs`
- Test: `tests/SymbolCommander.Core.Tests/ConfigStoreTests.cs` (append tests)

**Interfaces:**
- Consumes: existing config types.
- Produces: `ActionDefinition.Voice` (string, default `""`); `AppSettings.VoiceEnabled` (bool, default true); `AppSettings.VoiceVolume` (double, default 0.8); `ConfigStore.Directory` (string, the config dir — Task 5 derives the voice dir from it).

- [ ] **Step 1: Append the failing tests**

Append inside the `ConfigStoreTests` class in `tests/SymbolCommander.Core.Tests/ConfigStoreTests.cs`:

```csharp
    [Fact]
    public void Voice_fields_round_trip()
    {
        var store = new ConfigStore(_dir);
        var config = ConfigStore.DefaultConfig();
        config.Settings.VoiceEnabled = false;
        config.Settings.VoiceVolume = 0.35;
        config.Actions[0].Voice = "minimize.mp3";
        config.Actions[1].Voice = "none";
        store.Save(config);

        var loaded = new ConfigStore(_dir).Load();
        Assert.False(loaded.Settings.VoiceEnabled);
        Assert.Equal(0.35, loaded.Settings.VoiceVolume);
        Assert.Equal("minimize.mp3", loaded.Actions[0].Voice);
        Assert.Equal("none", loaded.Actions[1].Voice);
        Assert.Equal("", loaded.Actions[2].Voice); // untouched action defaults to Auto
    }

    [Fact]
    public void Pre_voice_config_loads_with_voice_defaults()
    {
        // a v0.1.x config knows nothing about voice fields
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "config.json"), """
        {
          "SchemaVersion": 1,
          "Settings": { "GesturesEnabled": true, "Sensitivity": 0.8 },
          "Actions": [ { "Id": "a1", "Name": "Undo", "Type": "Keystroke",
                         "Parameters": { "keys": "Ctrl+Z" } } ],
          "Bindings": [ { "SymbolId": "z", "ActionId": "a1", "Enabled": true } ]
        }
        """);
        var loaded = new ConfigStore(_dir).Load();
        Assert.True(loaded.Settings.VoiceEnabled);
        Assert.Equal(0.8, loaded.Settings.VoiceVolume);
        Assert.Equal("", loaded.Actions[0].Voice);
    }

    [Fact]
    public void Store_exposes_its_directory()
    {
        Assert.Equal(_dir, new ConfigStore(_dir).Directory);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/SymbolCommander.Core.Tests 2>&1 | tail -3
```

Expected: compile errors (`Voice`, `VoiceEnabled`, `VoiceVolume`, `Directory` undefined).

- [ ] **Step 3: Implement**

In `src/SymbolCommander.Core/Actions/ActionDefinition.cs`, add below the `Parameters` property:

```csharp
    /// <summary>"" = Auto (resolver picks), "none" = silent, else a voice-dir-relative file name.</summary>
    public string Voice { get; set; } = "";
```

In `src/SymbolCommander.Core/Config/AppSettings.cs`, add below `StartWithWindows`:

```csharp
    public bool VoiceEnabled { get; set; } = true;
    public double VoiceVolume { get; set; } = 0.8;
```

In `src/SymbolCommander.Core/Config/ConfigStore.cs`, add below the `ConfigPath` property:

```csharp
    public string Directory { get; }
```

CAREFUL: this class calls `System.IO.Directory` statically, and an instance property named
`Directory` shadows those calls inside the class. Qualify the three affected static calls:
- in `Save`: `System.IO.Directory.CreateDirectory(_dir);`
- in `LoadCustomSymbols`: `if (!System.IO.Directory.Exists(SymbolsDir)) return result;` and `foreach (var file in System.IO.Directory.GetFiles(SymbolsDir, "*.json"))`
- in `SaveCustomSymbol`: `System.IO.Directory.CreateDirectory(SymbolsDir);`

Initialize it in the constructor (replace the existing constructor line):

```csharp
    public ConfigStore(string directory)
    {
        _dir = directory;
        Directory = directory;
    }
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/SymbolCommander.Core.Tests 2>&1 | tail -3
```

Expected: all tests pass (65 total), 0 failed.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): voice fields on action/settings, expose config directory

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: VoiceResolver

**Files:**
- Create: `src/SymbolCommander.Core/Voice/VoiceResolver.cs`
- Test: `tests/SymbolCommander.Core.Tests/VoiceResolverTests.cs`

**Interfaces:**
- Consumes: `ActionDefinition` (Task 1).
- Produces (used by Tasks 4–6):
  - `enum VoiceResolutionKind { None, File, TtsNeeded }` in namespace `SymbolCommander.Core.Voice`
  - `sealed record VoiceResolution(VoiceResolutionKind Kind, string? FileName = null, string? TtsText = null)` with `static readonly VoiceResolution None`
  - `static class VoiceResolver`:
    - `const string NoneValue = "none"`
    - `string Slug(string name)` — lowercase, non-alphanumerics collapsed to single hyphens, trimmed
    - `VoiceResolution Resolve(ActionDefinition action, IReadOnlyCollection<string> availableFiles)` — files are voice-dir-relative, forward slashes

- [ ] **Step 1: Write the failing tests**

Write `tests/SymbolCommander.Core.Tests/VoiceResolverTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/SymbolCommander.Core.Tests 2>&1 | tail -3
```

Expected: compile errors (namespace `SymbolCommander.Core.Voice` doesn't exist).

- [ ] **Step 3: Implement**

Write `src/SymbolCommander.Core/Voice/VoiceResolver.cs`:

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/SymbolCommander.Core.Tests 2>&1 | tail -3
```

Expected: all pass (76 total), 0 failed.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): voice resolver with auto word-matching and tts fallback

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: VoicePackImporter

**Files:**
- Create: `src/SymbolCommander.Core/Voice/VoicePackImporter.cs`
- Test: `tests/SymbolCommander.Core.Tests/VoicePackImporterTests.cs`

**Interfaces:**
- Consumes: nothing new (uses `System.IO.Compression`, built into .NET).
- Produces (used by Task 6):
  - `static class VoicePackImporter`:
    - `int ImportZip(string zipPath, string voiceDir)` — returns files imported; throws `NotSupportedException` with a friendly message for unsupported compression (the original pack is LZMA)
    - `int ImportFolder(string folderPath, string voiceDir)` — recursive; returns files imported
  - Rules (both): only `.mp3`/`.wav`; flatten subfolders into the voice root; any file whose name contains "sensiva sound" (case-insensitive) lands as `startup.mp3`; existing files overwritten.

- [ ] **Step 1: Write the failing tests**

Write `tests/SymbolCommander.Core.Tests/VoicePackImporterTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/SymbolCommander.Core.Tests 2>&1 | tail -3
```

Expected: compile errors (`VoicePackImporter` undefined).

- [ ] **Step 3: Implement**

Write `src/SymbolCommander.Core/Voice/VoicePackImporter.cs`:

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/SymbolCommander.Core.Tests 2>&1 | tail -3
```

Expected: all pass (81 total), 0 failed.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): voice pack importer (zip/folder) with LZMA guidance

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: App voice components (player, TTS, service)

Windows-only code: verify by `dotnet build` on Linux; runtime behavior lands in the Task 7 checklist.

**Files:**
- Modify: `src/SymbolCommander.App/SymbolCommander.App.csproj` — add `System.Speech`
- Create: `src/SymbolCommander.App/Voice/VoicePlayer.cs`
- Create: `src/SymbolCommander.App/Voice/TtsSampleGenerator.cs`
- Create: `src/SymbolCommander.App/Voice/VoiceService.cs`

**Interfaces:**
- Consumes: `VoiceResolver`, `VoiceResolution(Kind)` (Task 2); `ActionDefinition` (Task 1).
- Produces (used by Tasks 5–6) — ALL playback calls must happen on the WPF UI thread (MediaPlayer thread affinity; VoiceService handles marshaling only for its internal TTS-generation path):
  - `sealed class VoicePlayer` — `void Play(string absolutePath, double volume)`; never throws
  - `static class TtsSampleGenerator` — `void RenderToFile(string text, string wavPath)`
  - `sealed class VoiceService` — ctor `(string voiceDir, Action<string,string,bool> notify)`:
    - `string VoiceDir { get; }`
    - `IReadOnlyList<string> AvailableFiles()` — relative, forward slashes, `.mp3`/`.wav` only
    - `void PlayForAction(ActionDefinition action, double volume)` — resolve → play; TtsNeeded generates on a background thread then plays
    - `void PlayStartup(double volume)` — plays root-level `startup.*` if present
    - `void PlayFile(string relativeName, double volume)`
    - `string? TryGenerateTts(string text)` — creates `tts/<slug>.wav`, returns relative name; on failure notifies once per session and returns null

- [ ] **Step 1: Add the System.Speech package**

```bash
dotnet add src/SymbolCommander.App package System.Speech --version 8.0.0
```

Expected: reference added; `dotnet build src/SymbolCommander.App` still succeeds.

- [ ] **Step 2: Write VoicePlayer**

Write `src/SymbolCommander.App/Voice/VoicePlayer.cs`:

```csharp
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
```

- [ ] **Step 3: Write TtsSampleGenerator**

Write `src/SymbolCommander.App/Voice/TtsSampleGenerator.cs`:

```csharp
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
```

- [ ] **Step 4: Write VoiceService**

Write `src/SymbolCommander.App/Voice/VoiceService.cs`:

```csharp
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
```

- [ ] **Step 5: Build and commit**

```bash
dotnet build src/SymbolCommander.App 2>&1 | grep -iE "error|Build succeeded|Build FAILED" | head -6
git add -A && git commit -m "feat(app): voice player, TTS sample generator, voice service

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: `Build succeeded. 0 Error(s)`.

---

### Task 5: Coordinator + tray + app wiring

**Files:**
- Modify: `src/SymbolCommander.App/Engine/GestureCoordinator.cs`
- Modify: `src/SymbolCommander.App/Tray/TrayIcon.cs`
- Modify: `src/SymbolCommander.App/App.xaml.cs`

**Interfaces:**
- Consumes: `VoiceService` (Task 4), `ConfigStore.Directory` (Task 1).
- Produces (used by Task 6): `GestureCoordinator.Voice` (`VoiceService`), `GestureCoordinator.SetVoiceEnabled(bool)`; `TrayIcon.VoiceToggled` event + `TrayIcon.SetVoiceEnabled(bool)`.

- [ ] **Step 1: Extend GestureCoordinator**

In `src/SymbolCommander.App/Engine/GestureCoordinator.cs`:

1. Add to the usings: `using SymbolCommander.App.Voice;`
2. Add a public property below `Catalog`:

```csharp
    public VoiceService Voice { get; }
```

3. In the constructor, right after `_notify = notify;` add:

```csharp
        Voice = new VoiceService(System.IO.Path.Combine(store.Directory, "voice"), notify);
```

4. In `Start()`, after `_hookHost.Start();` add:

```csharp
        var settings = CurrentConfig.Settings;
        if (settings.VoiceEnabled) Voice.PlayStartup(settings.VoiceVolume);
```

(`Start()` runs on the UI thread in `App.OnStartup` — required for MediaPlayer.)

5. Add below `SetGesturesEnabled`:

```csharp
    public void SetVoiceEnabled(bool on)
    {
        AppConfig config;
        lock (_gate) { _config.Settings.VoiceEnabled = on; config = _config; }
        _store.Save(config);
    }
```

6. In `OnStrokeCompleted`, extend the snapshot block — add two locals inside the `lock (_gate)`:

```csharp
            bool voiceOn = _config.Settings.VoiceEnabled;
            double voiceVolume = _config.Settings.VoiceVolume;
```

(declare `bool voiceOn; double voiceVolume;` alongside the existing pre-lock declarations)
and after `_executor.Execute(action);` add:

```csharp
            if (voiceOn) Voice.PlayForAction(action, voiceVolume);
```

(`OnStrokeCompleted` already runs on the UI thread via `OnUi` — safe for playback.)

- [ ] **Step 2: Extend TrayIcon**

In `src/SymbolCommander.App/Tray/TrayIcon.cs`:

1. Add a field next to `_enabledItem`:

```csharp
    private readonly ToolStripMenuItem _voiceItem;
```

2. Add an event next to `GesturesToggled`:

```csharp
    public event Action<bool>? VoiceToggled;
```

3. In the constructor, after the `_enabledItem` wiring add:

```csharp
        _voiceItem = new ToolStripMenuItem("Voice enabled") { Checked = true, CheckOnClick = true };
        _voiceItem.CheckedChanged += (_, _) => VoiceToggled?.Invoke(_voiceItem.Checked);
```

and change the menu construction line to include it:

```csharp
        menu.Items.AddRange(new ToolStripItem[]
            { _enabledItem, _voiceItem, new ToolStripSeparator(), settings, new ToolStripSeparator(), exit });
```

4. Add below `SetGesturesEnabled`:

```csharp
    public void SetVoiceEnabled(bool on) => _voiceItem.Checked = on;
```

- [ ] **Step 3: Wire in App.xaml.cs**

In `src/SymbolCommander.App/App.xaml.cs`, after the line
`Tray.SetGesturesEnabled(Coordinator.CurrentConfig.Settings.GesturesEnabled);` add:

```csharp
        Tray.SetVoiceEnabled(Coordinator.CurrentConfig.Settings.VoiceEnabled);
```

and after `Tray.GesturesToggled += on => Coordinator.SetGesturesEnabled(on);` add:

```csharp
        Tray.VoiceToggled += on => Coordinator.SetVoiceEnabled(on);
```

- [ ] **Step 4: Build, test, commit**

```bash
dotnet build src/SymbolCommander.App 2>&1 | grep -iE "error|Build succeeded|Build FAILED" | head -6
dotnet test tests/SymbolCommander.Core.Tests 2>&1 | tail -3
git add -A && git commit -m "feat(app): voice playback wired into coordinator, tray voice toggle, startup jingle

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: build succeeded; all Core tests pass.

---

### Task 6: Settings UI — Voice group + action editor Voice row

**Files:**
- Modify: `src/SymbolCommander.App/Settings/GeneralTab.xaml` + `.xaml.cs`
- Modify: `src/SymbolCommander.App/Settings/ActionEditorDialog.xaml` + `.xaml.cs`

**Interfaces:**
- Consumes: `Coordinator.Voice` (Task 5), `VoicePackImporter` (Task 3), `VoiceResolver` (Task 2), the SettingsWindow dirty-tracking convention (`_owner.MarkDirty()`; loads run under suppression).
- Produces: user-facing controls only.

- [ ] **Step 1: Add the Voice group to GeneralTab.xaml**

In `src/SymbolCommander.App/Settings/GeneralTab.xaml`, insert after the closing tag of the "System" GroupBox (before the "Actions" GroupBox):

```xml
            <GroupBox Header="Voice" Padding="8" Margin="0,8,0,0">
                <StackPanel>
                    <StackPanel Orientation="Horizontal">
                        <CheckBox x:Name="VoiceCheck" Content="Voice feedback" VerticalAlignment="Center"/>
                        <TextBlock Text="Volume:" VerticalAlignment="Center" Margin="16,0,0,0"/>
                        <Slider x:Name="VoiceVolumeSlider" Width="140" Minimum="0" Maximum="1"
                                TickFrequency="0.05" Margin="8,0"/>
                    </StackPanel>
                    <StackPanel Orientation="Horizontal" Margin="0,6,0,0">
                        <Button Content="Import zip…" Width="100" Click="ImportZip_Click"/>
                        <Button Content="Import folder…" Width="110" Margin="6,0" Click="ImportFolder_Click"/>
                        <TextBlock x:Name="VoiceStatusLabel" VerticalAlignment="Center" Margin="10,0,0,0"
                                   Foreground="Gray"/>
                    </StackPanel>
                </StackPanel>
            </GroupBox>
```

- [ ] **Step 2: Wire the Voice group in GeneralTab.xaml.cs**

In `src/SymbolCommander.App/Settings/GeneralTab.xaml.cs`:

1. Add to the usings: `using SymbolCommander.Core.Voice;`
2. In the constructor, next to the other MarkDirty wiring, add:

```csharp
        VoiceCheck.Click += (_, _) => _owner?.MarkDirty();
        VoiceVolumeSlider.ValueChanged += (_, _) => _owner?.MarkDirty();
```

3. In `Load(SettingsWindow owner)`, before `RefreshActions();` add:

```csharp
        VoiceCheck.IsChecked = s.VoiceEnabled;
        VoiceVolumeSlider.Value = s.VoiceVolume;
        UpdateVoiceStatus();
```

4. In `CollectInto(AppConfig working)`, at the end of the method add:

```csharp
        s.VoiceEnabled = VoiceCheck.IsChecked == true;
        s.VoiceVolume = VoiceVolumeSlider.Value;
```

5. Add the handlers inside the class (imports act immediately and do NOT touch dirty state):

```csharp
    private void ImportZip_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
            { Filter = "Zip archives|*.zip", Title = "Import voice pack" };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;
        RunImport(() => VoicePackImporter.ImportZip(dlg.FileName, _owner.Coordinator.Voice.VoiceDir));
    }

    private void ImportFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "Import voice pack folder" };
        if (dlg.ShowDialog(Window.GetWindow(this)) != true) return;
        RunImport(() => VoicePackImporter.ImportFolder(dlg.FolderName, _owner.Coordinator.Voice.VoiceDir));
    }

    private void RunImport(Func<int> import)
    {
        try
        {
            int n = import();
            UpdateVoiceStatus();
            MessageBox.Show($"Imported {n} voice sample(s).", "Symbol Commander",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void UpdateVoiceStatus() =>
        VoiceStatusLabel.Text = $"{_owner.Coordinator.Voice.AvailableFiles().Count} voice samples available";
```

- [ ] **Step 3: Add the Voice row to ActionEditorDialog.xaml**

In `src/SymbolCommander.App/Settings/ActionEditorDialog.xaml`, insert immediately BEFORE the `<TextBlock x:Name="ErrorLabel" …/>` line:

```xml
        <TextBlock Text="Voice:"/>
        <DockPanel Margin="0,2,0,8">
            <Button DockPanel.Dock="Right" Content="Add file…" Width="80" Margin="6,0,0,0" Click="AddVoiceFile_Click"/>
            <Button DockPanel.Dock="Right" Content="TTS" Width="46" Margin="6,0,0,0" Click="GenerateTts_Click"
                    ToolTip="Generate a spoken sample from the action name"/>
            <Button DockPanel.Dock="Right" Content="▶" Width="32" Margin="6,0,0,0" Click="PreviewVoice_Click"
                    ToolTip="Preview"/>
            <ComboBox x:Name="VoiceCombo"/>
        </DockPanel>
```

- [ ] **Step 4: Wire the Voice row in ActionEditorDialog.xaml.cs**

In `src/SymbolCommander.App/Settings/ActionEditorDialog.xaml.cs`:

1. Add to the usings: `using System.IO;` and `using SymbolCommander.Core.Voice;`
2. Add inside the class:

```csharp
    private sealed record VoiceChoice(string Value, string Display)
    {
        public override string ToString() => Display;
    }

    private string SelectedVoiceValue() =>
        (VoiceCombo.SelectedItem as VoiceChoice)?.Value ?? "";

    private void RefreshVoiceChoices(string select)
    {
        var svc = App.Current.Coordinator.Voice;
        var files = svc.AvailableFiles();

        var autoProbe = new ActionDefinition { Name = NameBox.Text.Trim(), Voice = "" };
        var auto = VoiceResolver.Resolve(autoProbe, files);
        string autoDisplay = auto.Kind == VoiceResolutionKind.File
            ? $"Auto (→ {auto.FileName})"
            : $"Auto (→ TTS: \"{autoProbe.Name}\")";

        var choices = new List<VoiceChoice>
        {
            new("", autoDisplay),
            new(VoiceResolver.NoneValue, "None"),
        };
        choices.AddRange(files.Select(f => new VoiceChoice(f, f)));
        if (select is not ("" or VoiceResolver.NoneValue)
            && !files.Contains(select, StringComparer.OrdinalIgnoreCase))
            choices.Add(new VoiceChoice(select, $"{select} (missing)"));

        VoiceCombo.ItemsSource = choices;
        VoiceCombo.SelectedItem =
            choices.FirstOrDefault(c => c.Value.Equals(select, StringComparison.OrdinalIgnoreCase))
            ?? choices[0];
    }

    private void PreviewVoice_Click(object sender, RoutedEventArgs e)
    {
        var svc = App.Current.Coordinator.Voice;
        double volume = App.Current.Coordinator.CurrentConfig.Settings.VoiceVolume;
        var probe = new ActionDefinition { Name = NameBox.Text.Trim(), Voice = SelectedVoiceValue() };
        var r = VoiceResolver.Resolve(probe, svc.AvailableFiles());
        if (r.Kind == VoiceResolutionKind.File)
        {
            svc.PlayFile(r.FileName!, volume);
        }
        else if (r.Kind == VoiceResolutionKind.TtsNeeded)
        {
            var rel = svc.TryGenerateTts(r.TtsText!);
            if (rel is not null)
            {
                svc.PlayFile(rel, volume);
                RefreshVoiceChoices(SelectedVoiceValue());
            }
        }
    }

    private void GenerateTts_Click(object sender, RoutedEventArgs e)
    {
        var rel = App.Current.Coordinator.Voice.TryGenerateTts(NameBox.Text.Trim());
        if (rel is not null) RefreshVoiceChoices(select: rel);
    }

    private void AddVoiceFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
            { Filter = "Audio files|*.mp3;*.wav", Title = "Add voice sample" };
        if (dlg.ShowDialog(this) != true) return;
        var svc = App.Current.Coordinator.Voice;
        Directory.CreateDirectory(svc.VoiceDir);
        var name = Path.GetFileName(dlg.FileName);
        File.Copy(dlg.FileName, Path.Combine(svc.VoiceDir, name), overwrite: true);
        RefreshVoiceChoices(select: name);
    }
```

3. In the constructor, after `UpdatePanels();` add:

```csharp
        RefreshVoiceChoices(existing?.Voice ?? "");
```

4. In `Ok_Click`, extend the object initializer to carry the voice value — replace

```csharp
        var action = new ActionDefinition { Id = _id, Name = NameBox.Text.Trim(), Type = t };
```

with

```csharp
        var action = new ActionDefinition
            { Id = _id, Name = NameBox.Text.Trim(), Type = t, Voice = SelectedVoiceValue() };
```

Note: `Directory`/`File` here are `System.IO` — this file previously had no `System.IO` using, and WinForms interop does not collide with these names. If the build reports an ambiguity with `Path`, qualify as `System.IO.Path`.

- [ ] **Step 5: Build, test, commit**

```bash
dotnet build src/SymbolCommander.App 2>&1 | grep -iE "error|Build succeeded|Build FAILED" | head -8
dotnet test tests/SymbolCommander.Core.Tests 2>&1 | tail -3
git add -A && git commit -m "feat(app): voice settings group and per-action voice editor

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: build succeeded; all Core tests pass.

---

### Task 7: README, publish, Windows checklist

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add a Voice section to README.md**

Insert after the "## Use" section:

```markdown
## Voice feedback

Actions can speak when they fire, Sensiva-style. Voice is on by default
(toggle in Settings → Actions & General, or the tray menu; volume slider in settings).

- **Original clips:** Settings → "Import folder…" and point at your extracted
  "English Female Voice" pack (the original zip is LZMA-compressed, which .NET can't
  read — extract it first; "Import zip…" works for ordinary zips). Clips land in
  `%APPDATA%\SymbolCommander\voice\`; "The Sensiva Sound" becomes the startup jingle.
- **Auto matching:** an action picks the clip whose name appears in the action's name
  ("Minimize window" → minimize.mp3). No match → a sample is generated once with
  Windows TTS and reused.
- **Per action:** the action editor's Voice row lets you pick any clip, generate a
  TTS sample, add your own .mp3/.wav, preview, or silence that action.

No copyrighted audio ships with the app or this repo.
```

- [ ] **Step 2: Full verification + publish**

```bash
./publish.sh 2>&1 | grep -iE "Passed!|Failed|Built:"
```

Expected: all Core tests pass; exe built.

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "docs: voice feedback section in README

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

- [ ] **Step 4: WINDOWS CHECKLIST (user, on the Windows PC)**

Copy the fresh exe over, plus the **extracted** voice-pack folder. Verify:

1. Launch → no jingle yet (no clips imported), no errors.
2. Settings → Actions & General → "Import folder…" → pick the extracted "English Female Voice" folder → "Imported 35 voice sample(s)" (34 clips + "The Sensiva Sound" → startup.mp3; the `events` file is skipped), status label updates.
3. Try "Import zip…" with the ORIGINAL zip → friendly error mentioning "Import folder…".
4. Draw `M` → hears "minimize" as the window minimizes. Draw `←` → "back".
5. Draw `Z` (Undo — no clip) → first fire generates a TTS "Undo" (sub-second delay), later fires play instantly.
6. Restart the app → startup jingle plays.
7. Action editor: Voice dropdown shows "Auto (→ minimize.mp3)" for Minimize window; pick a different clip → Apply → gesture speaks the new clip. ▶ previews. "TTS" generates + selects a sample. "Add file…" imports a custom .mp3/.wav and selects it. "None" silences just that action.
8. Tray → untick "Voice enabled" → silence (gestures still work); re-tick → voice returns. Settings checkbox + volume slider behave; Apply dirty-tracking lights up for them but NOT for imports.
9. Old config upgrade: this build over the v0.1.1 config → voice defaults on, nothing lost.

Fix anything reported, then merge via the finishing-a-development-branch flow and cut the **v0.2.0** release (same `gh release create` pattern as v0.1.1, fresh exe attached) — only after the checklist passes.

## Execution notes

- Tasks 1–3 are TDD on Linux; Tasks 4–6 compile-verify only; Task 7 gates the release on the user's Windows verification.
- If a later task touches Core, re-run `dotnet test tests/SymbolCommander.Core.Tests` before committing.
