# Symbol Commander — Design Spec

**Date:** 2026-07-07
**Status:** Approved by user (brainstorming session)

## What & why

A modern recreation of Sensiva Symbol Commander: a Windows tray utility that recognizes
mouse-drawn symbols (letters, arrows, shapes) and fires user-defined actions. Users can
bind actions to built-in symbols, train their own custom symbols, and create their own
actions. Works with both mice and laptop trackpads. Runs fully locally.

## Decisions made

| Topic | Decision |
|---|---|
| Gesture style | Unistroke symbol recognition (Sensiva-faithful), $1-family recognizer |
| Trackpad support | Pointer-based drawing; hotkey-hold trigger as the trackpad-friendly mode |
| Action types | Keystrokes, launch app/file/URL, window & media controls, shell commands |
| Trigger | Configurable; right-mouse-button hold AND hotkey hold, both enabled by default |
| Gesture creation | Built-in symbol library + user-trained custom symbols (3–5 drawn examples) |
| Binding scope | Global only in v1 (per-app profiles are a possible later addition) |
| Stack | C# / .NET 8, WPF, single tray app |
| Dev environment | Developed on Linux (`EnableWindowsTargeting`), tested on a separate Windows PC |
| Distribution | Portable single self-contained exe for v1; no installer |

## Architecture

One solution, two projects (plus tests):

### `SymbolCommander.Core` — class library, no Windows dependencies

Unit-testable on Linux. Contains:

- **Recognizer** — $1-family unistroke recognizer, Protractor variant (closed-form
  angular match; fast and trainable from examples).
  - Pipeline: raw points → resample to 64 evenly spaced points → normalize
    (scale to unit square preserving aspect, translate centroid to origin) →
    Protractor match against every enabled template → best score wins if above the
    confidence threshold.
  - Threshold is user-adjustable ("sensitivity" slider). Below threshold → no match.
  - Strokes shorter than ~30 px total path length or with fewer than ~5 raw points
    are rejected as accidental.
- **Gesture library model** — built-in templates (letters M, W, S, etc.; arrows in four
  directions; shapes: triangle, circle, Z, question-mark, etc.) plus user templates.
  A custom symbol stores its 3–5 drawn examples as point arrays; each example is a
  template for matching (no ML, instant and predictable).
- **Action model** — `ActionDefinition { id, name, type, parameters }` where type ∈
  { Keystroke, Launch, WindowMedia, Shell }. `Binding { symbolId, actionId, enabled }`.
- **Config store** — JSON serialization for settings, actions, bindings, custom symbols.

### `SymbolCommander.App` — WPF, Windows-only

Thin OS-facing shell:

- **Tray icon** — enable/disable toggle, Settings, Exit. No taskbar presence.
  Optional start-with-Windows via HKCU Run key. Single-instance mutex; a second launch
  focuses the settings window.
- **Input hook** — `WH_MOUSE_LL` + `WH_KEYBOARD_LL` via P/Invoke on a dedicated thread.
  - While a trigger is held, pointer moves are buffered into a point list and the
    trigger input is suppressed.
  - Hook callbacks only buffer and forward — no recognition or action work inside the
    callback (Windows silently drops slow hooks). Recognition/actions run elsewhere.
  - Watchdog re-installs the hook if Windows removes it.
- **Right-click passthrough** — with the right-button trigger, button-down is withheld.
  If pointer moves < ~10 px before release, replay a normal right-click (context menus
  still work). If ≥ threshold, it is a gesture and the click is swallowed.
- **Hotkey-hold trigger** — hold a configurable modifier combo (default Ctrl+Alt) and
  draw with the pointer; identical downstream flow. This is the trackpad-friendly mode.
- **Overlay** — click-through, transparent, topmost full-screen WPF window (spanning all
  monitors, DPI-aware). Draws the ink trail live (configurable color/width). On
  recognition: brief toast near the pointer with symbol + action name. On no-match:
  trail fades red. Escape cancels an in-progress stroke.
- **Action executor**
  - Keystroke: `SendInput` scan-code sequences (recorded via a keystroke-recorder box).
  - Launch: `Process.Start` with UseShellExecute (apps, files, folders, URLs).
  - WindowMedia: `ShowWindow`/`SC_*` messages for minimize/maximize/close/restore/snap
    of the foreground window; `APPCOMMAND` messages for volume/mute/play-pause/next/prev;
    `LockWorkStation` for lock.
  - Shell: `Process.Start` of the configured command line, non-elevated, hidden or
    visible per action setting. Failures toast, never crash.
- **Settings window** — three tabs:
  1. **Bindings** — symbol → action table; add/edit/remove/enable; test-draw canvas that
     shows what recognizes without firing.
  2. **Symbols** — built-in library + custom; "New symbol" flow: draw 3–5 examples,
     live consistency check, collision warning if too similar to an existing symbol.
  3. **Actions & general** — action editor for the four types; trigger config; trail
     appearance; sensitivity slider; start-with-Windows.

## Data flow

```
hook thread: trigger held → buffer points → trigger released
    → post stroke to app thread
app thread: preprocess/validate stroke → Core.Recognizer.Match(templates)
    → match: overlay toast + ActionExecutor.Run(binding.action)
    → no match: overlay red fade
settings changes → ConfigStore.Save → engine hot-reloads bindings/templates
```

## Configuration & persistence

- `%APPDATA%\SymbolCommander\config.json` — settings, actions, bindings.
- `%APPDATA%\SymbolCommander\symbols\*.json` — custom symbol templates.
- `schemaVersion` field for future migrations.
- Atomic writes (temp file + rename). Corrupt file on load → back it up aside, load
  defaults, tray notification. Never a crash loop.

## Error handling summary

- Slow-hook protection + watchdog (above).
- Action failures (missing exe, bad command) → tray toast with reason.
- Recognizer never throws on degenerate strokes; they are rejected pre-match.
- Config corruption → backup + defaults + notification.

## Testing strategy

- **Linux (automated):** `SymbolCommander.Core.Tests` (xUnit) — recognizer accuracy
  against fixture strokes (recorded point arrays for every built-in symbol, plus sloppy
  variants), junk-stroke rejection, threshold behavior, config round-trip, binding
  resolution, custom-template training consistency.
- **Windows PC (manual checklist per milestone):** trigger + right-click passthrough,
  trail rendering on multi-monitor and DPI-scaled displays, each action type fires,
  hotkey-hold trackpad flow, tray lifecycle, start-with-Windows, single-instance.
- **Build/ship loop:** `dotnet publish -c Release -r win-x64 --self-contained` with
  `<EnableWindowsTargeting>true</EnableWindowsTargeting>` → copy the exe to the PC.

## Milestones

1. Core recognizer + unit tests (Linux-verifiable).
2. Hook + overlay + one hardcoded binding — first end-to-end gesture on Windows.
3. Action executor (all four types) + config store.
4. Settings UI: bindings + actions tabs.
5. Custom symbol training (symbols tab) + collision check.
6. Polish: DPI/multi-monitor, start-with-Windows, single-instance, packaging.

## Out of scope for v1

- Per-app binding profiles (design leaves room: bindings gain an optional app filter later).
- Multi-finger / Precision-Touchpad raw input.
- Installer/auto-update.
- Multi-stroke symbols (unistroke only, by construction of the hold-draw-release flow).
