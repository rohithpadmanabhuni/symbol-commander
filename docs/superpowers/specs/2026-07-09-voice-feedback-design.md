# Voice Feedback — Design Spec

**Date:** 2026-07-09
**Status:** Approved by user (brainstorming session)
**Target release:** v0.2.0

## What & why

Sensiva-style voice feedback: when a gesture fires an action, a short voice clip speaks
it ("minimize", "back"…), recreating the original product's signature feel. The user's
original "English Female Voice" pack (38 MP3 clips + an `events` id→clip map) supplies
the classic clips; Windows TTS fills the gaps for actions the pack doesn't cover and for
new user-created actions. Voice can be enabled/disabled globally and managed per action.

## Decisions made

| Topic | Decision |
|---|---|
| When voice plays | On action fire + startup jingle. Rejected strokes stay silent |
| Per-action control | User can change, add, or remove the sound for any action |
| Clip distribution | Voice-pack folder in `%APPDATA%\SymbolCommander\voice\` with import UI; copyrighted clips are NOT committed to the public repo or embedded in the exe |
| TTS | `System.Speech` (SAPI) rendering WAV samples once to disk; played like any clip. One new NuGet package (`System.Speech`, Microsoft) — deliberate exception to the zero-dependency rule |
| Resolution model | Auto + override: name-matching resolver by default; per-action explicit clip / TTS sample / own audio file / None |
| Controls | Settings checkbox + tray "Voice enabled" toggle + volume slider. Default ON |

## Storage layout

Everything is a file; original clips, imported audio, and TTS output are treated identically.

```
%APPDATA%\SymbolCommander\voice\        clips (.mp3/.wav): minimize.mp3, back.mp3, startup.mp3, …
%APPDATA%\SymbolCommander\voice\tts\    generated samples: volume-up.wav, undo.wav, …
```

The startup jingle is any file named `startup.*` in the voice root. The importer renames
"The Sensiva Sound.mp3" to `startup.mp3` on ingest.

## Core changes (Linux-testable)

### Model
- `ActionDefinition.Voice` (string):
  - `""` (default) = **Auto** — old configs deserialize to Auto automatically.
  - `"none"` = silent.
  - anything else = a file name relative to the voice folder (e.g. `minimize.mp3`, `tts/undo.wav`).
- `AppSettings.VoiceEnabled` (bool, default `true`), `AppSettings.VoiceVolume` (double 0–1, default `0.8`).
- Config `SchemaVersion` stays 1 (additive change; missing properties take defaults).

### VoiceResolver (pure static class, fully unit-tested)

`Resolve(action, availableFiles) → VoiceResolution { Kind: None | File | TtsNeeded, FileName?, TtsText? }`

- `Voice == "none"` → None.
- `Voice == <file>` and file exists → File. If the file is missing → fall through to Auto
  (predictable degradation; the action editor shows what Auto resolves to).
- **Auto:**
  1. A clip in the voice root matches if its base name (case-insensitive, extension stripped)
     appears as a whole word in the action name: "Minimize window" → `minimize.mp3`,
     "Back" → `back.mp3`, "Show desktop" → `desktop.mp3`, "Open browser" → `open.mp3`.
     Longest matching base name wins ties (`website` beats `web`).
  2. Else `tts/<slug>.wav` if present, where `<slug>` = action name lowercased,
     non-alphanumerics collapsed to single hyphens (e.g. "Volume up" → `volume-up`).
  3. Else `TtsNeeded(actionName)` — the caller generates the sample, then plays it.
- Slug derivation is exposed as `VoiceResolver.Slug(name)` so generator and resolver agree.

### VoicePackImporter (Core, unit-tested)

- `ImportZip(zipPath, voiceDir)` — extracts via `System.IO.Compression.ZipArchive`;
  copies only `.mp3`/`.wav`, flattening subfolders (the pack's `services/` clips land in
  the root); skips everything else (e.g. the `events` file); any file whose name contains
  "sensiva sound" (case-insensitive) becomes `startup.mp3`. Existing files are overwritten.
- `ImportFolder(folderPath, voiceDir)` — same rules, recursive copy.
- **Known limitation (discovered):** the user's original zip is LZMA-compressed (method 14),
  which `ZipArchive` cannot read. `ImportZip` catches this and throws a
  `NotSupportedException` with the message telling the user to extract the zip and use
  "Import folder…". No third-party archive library is added.
- Both return the count of files imported (shown in the UI).

## App changes (Windows)

### VoicePlayer
- Wraps a single WPF `MediaPlayer` (plays MP3 and WAV natively, no dependency).
- `Play(absolutePath, volume)` — stops any current playback, opens, sets volume, plays.
  Latest-wins is correct for ~1-second lines.
- `MediaFailed` → log and ignore. Voice must never break gestures.

### TtsSampleGenerator
- `System.Speech.Synthesis.SpeechSynthesizer` → `SetOutputToWaveFile(voice/tts/<slug>.wav)`,
  speak the action name, dispose. Uses the default installed SAPI voice.
- Runs on a background thread. First fire of an uncovered action: generate (~sub-second),
  then play on completion; subsequent fires hit the cached file.
- If synthesis fails (no SAPI voice installed): one tray toast per session, resolution
  becomes silent for that session.

### Coordinator integration
- After `executor.Execute(action)`: resolve → play (or generate-then-play). All gated on
  `VoiceEnabled`, volume from settings snapshot under the existing `_gate` hot-reload lock.
- On `Start()`: if voice enabled and `startup.*` exists → play it once.
- Available-file list is rescanned on `ApplyConfig` and after imports (cheap directory list).

### Settings UI
- **Actions & General tab — new "Voice" GroupBox:** `[x] Voice feedback` checkbox,
  volume slider (0–100%), "Import zip…" and "Import folder…" buttons (WPF
  `OpenFileDialog` / `OpenFolderDialog`), status line "N voice samples available".
  The checkbox and slider participate in the existing MarkDirty tracking (config
  values, saved on Apply). The import buttons are file operations, not config —
  they act immediately and do not touch the dirty state.
- **Tray menu:** "Voice enabled" check item next to "Gestures enabled"; toggling persists
  to config immediately (mirrors the gestures toggle behavior).
- **ActionEditorDialog — new "Voice" row:**
  - Dropdown: `Auto (→ minimize.mp3)` showing the live Auto resolution, `None`,
    every clip in the voice root, and this action's TTS sample if it exists.
  - **▶ Preview** — plays the currently selected resolution.
  - **Generate TTS sample** — creates/regenerates `tts/<slug>.wav` from the action name
    and selects it.
  - **Add audio file…** — copies a chosen `.mp3`/`.wav` into the voice folder and selects it.
  - Maps to `ActionDefinition.Voice`: `""`, `"none"`, or the file name.

## Error handling summary

- Playback failure → skip silently (log only). Never crash, never block the action.
- TTS unavailable → one toast per session, then silent.
- Import failure (including LZMA zip) → message box with the reason and the
  extract-and-import-folder workaround.
- Volume clamped to [0, 1]. Missing voice folder = zero files = TTS-only behavior.

## Testing strategy

- **Linux (automated, Core):**
  - `VoiceResolver`: auto word-match, longest-name tie-break, explicit file, explicit
    missing file → Auto fallback, `none`, TTS slug rules, tts-sample preference order.
  - `VoicePackImporter`: a deflate zip fixture built inside the test (ZipArchive-created),
    folder import, subfolder flattening, sensiva-sound → `startup.mp3` rename, non-audio
    skipped, imported-count return values, and the LZMA case: the test builds a valid
    deflate zip then patches its compression-method bytes (local + central headers) to 14,
    asserting `ImportZip` surfaces the friendly `NotSupportedException`.
  - Config: round-trip of `Voice`/`VoiceEnabled`/`VoiceVolume`; a pre-voice config JSON
    (no new fields) loads with defaults (enabled, 0.8, Auto).
- **Windows (manual checklist):** import the real pack via extracted folder; M-gesture
  speaks "minimize"; startup jingle on launch; "Volume up" generates + speaks a TTS sample
  on first fire and reuses it after; action-editor dropdown/preview/generate/add-file;
  tray voice toggle mutes instantly; volume slider works; Apply dirty-tracking on new
  controls; LZMA zip import shows the friendly error.

## Out of scope for v1 of this feature

- Multiple voice packs / pack switching.
- Per-action volume.
- Rejection/unrecognized-stroke sounds.
- Bundling any copyrighted audio in the repo, exe, or GitHub release.
- WinRT/natural voices and embedded neural TTS.
