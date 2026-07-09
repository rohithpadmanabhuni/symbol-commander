# Symbol Commander

A recreation of Sensiva Symbol Commander for modern Windows: draw symbols with
your mouse or trackpad and fire actions — open apps, send hotkeys, control
windows and media, run commands.

## Use

- **Mouse:** hold the **right button** and draw a symbol (M, W, a circle, an
  arrow stroke…). Release to fire the bound action. A quick right-click without
  movement is still a normal right-click.
- **Trackpad:** hold **Ctrl+Alt** and draw with a one-finger glide, then release.
- **Esc** cancels a stroke mid-draw. The tray icon toggles gestures on/off.
- Right-click the tray icon → **Settings** to edit bindings, create actions
  (keystrokes / launch / window & media / shell), and train your own symbols
  by drawing 3–5 examples.

Config lives in `%APPDATA%\SymbolCommander`. Portable: a single exe, no installer.

## Default bindings

| Symbol | Action |
|---|---|
| W | Open browser |
| M | Minimize window |
| Z | Undo (Ctrl+Z) |
| ↑ / ↓ | Volume up / down |
| ← / → | Back / Forward |
| ○ (circle) | Show desktop |

## Build

Requires the .NET 8 SDK. Works on Windows or Linux (cross-build):

    ./publish.sh

Output: `src/SymbolCommander.App/bin/Release/net8.0-windows/win-x64/publish/SymbolCommander.exe`

Run tests only: `dotnet test`

## Architecture

- `src/SymbolCommander.Core` — platform-free: Protractor unistroke recognizer
  (±30° rotation tolerance), trigger state machine, action/config models. All
  62 unit tests live here and run on any OS.
- `src/SymbolCommander.App` — Windows shell: low-level hooks (WH_MOUSE_LL /
  WH_KEYBOARD_LL) on a watchdog-guarded thread, click-through WPF overlay for
  the ink trail, SendInput-based action executor, tray icon, settings UI.

Design docs: `docs/superpowers/specs/`, plan: `docs/superpowers/plans/`.
