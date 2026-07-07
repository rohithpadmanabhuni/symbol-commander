# Symbol Commander Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Windows tray utility that recognizes mouse/trackpad-drawn symbols and fires user-defined actions (recreation of Sensiva Symbol Commander).

**Architecture:** One solution, two projects. `SymbolCommander.Core` (netstandard-style net8.0 class library, zero Windows dependencies) holds the Protractor gesture recognizer, the trigger state machine, the action/config models — all unit-tested on this Linux box. `SymbolCommander.App` (net8.0-windows WPF) is a thin shell: low-level hooks, transparent overlay, SendInput action executor, tray icon, settings window. Cross-built from Linux with `EnableWindowsTargeting`; manually verified on a separate Windows PC at two milestones.

**Tech Stack:** .NET 8 SDK, C#, WPF + WinForms interop (NotifyIcon only), xUnit. Zero runtime NuGet dependencies.

**Spec:** `docs/superpowers/specs/2026-07-07-symbol-commander-design.md` (approved). Read it before starting if anything here seems ambiguous.

## Global Constraints

- .NET 8 SDK installed at `~/.dotnet` (Task 1). Every shell command below assumes: `export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"` (Task 1 also adds this to `~/.bashrc`; include the export inline if a fresh shell doesn't have it).
- Repo root (run all commands from here): `/home/dev1/Rohith/symbol-commander`
- `SymbolCommander.Core` MUST NOT reference any Windows API, WPF, or WinForms type. It must build and test on Linux.
- Zero runtime NuGet packages. Test project uses only what `dotnet new xunit` generates.
- JSON via `System.Text.Json` with `WriteIndented = true` and `JsonStringEnumConverter`.
- Config directory on Windows: `%APPDATA%\SymbolCommander` (code: `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` + `"SymbolCommander"`).
- Recognizer constants (from spec): resample to **64** points; min stroke path length **30 px**; min raw points **5**; rotation tolerance clamp **±30°**; default sensitivity threshold **0.80** (user range 0.50–0.95).
- Right-click passthrough movement threshold: **10 px**.
- All C# files: file-scoped namespaces, `nullable enable` (project-wide), 4-space indent.
- Commit after every task (messages given per task). Git identity is already configured repo-local.
- Windows-only code (`SymbolCommander.App`) is verified on Linux by `dotnet build` only; real behavior is checked on the user's Windows PC at the two milestone checklists (Task 8 mini-check, Task 12 milestone, Task 17 final). Do not claim runtime behavior works until those checklists pass.

## Spec milestones → tasks

| Spec milestone | Tasks |
|---|---|
| 1. Core recognizer + tests | 1–7 |
| 2. Hook + overlay + first end-to-end gesture | 8–10, 12 |
| 3. Actions + config | 6, 7, 11, 12 |
| 4. Settings UI (bindings + actions) | 13–15 |
| 5. Custom symbol training | 16 |
| 6. Polish + packaging | 17 |

## File structure

```
symbol-commander/
├── SymbolCommander.sln
├── .gitignore
├── publish.sh                                  # Task 17
├── README.md                                   # Task 17
├── src/
│   ├── SymbolCommander.Core/
│   │   ├── SymbolCommander.Core.csproj
│   │   ├── Recognition/
│   │   │   ├── GesturePoint.cs                 # Task 2 — point record struct
│   │   │   ├── StrokePreprocessor.cs           # Task 2 — validate/resample/normalize/vectorize
│   │   │   ├── SymbolTemplate.cs               # Task 3 — (SymbolId, Vector)
│   │   │   ├── RecognitionResult.cs            # Task 3
│   │   │   └── ProtractorRecognizer.cs         # Task 3 — closed-form match, clamped rotation
│   │   ├── Library/
│   │   │   └── BuiltInSymbols.cs               # Task 4 — shipped symbol shapes
│   │   ├── Engine/
│   │   │   ├── TriggerSource.cs                # Task 5
│   │   │   └── GestureEngine.cs                # Task 5 — trigger/passthrough state machine
│   │   ├── Actions/
│   │   │   ├── ActionType.cs                   # Task 6
│   │   │   ├── WindowMediaCommand.cs           # Task 6
│   │   │   ├── ActionDefinition.cs             # Task 6
│   │   │   ├── Binding.cs                      # Task 6
│   │   │   ├── KeystrokeParser.cs              # Task 6 — "Ctrl+Shift+T, Ctrl+K" → combos
│   │   │   └── ActionValidator.cs              # Task 6
│   │   └── Config/
│   │       ├── AppSettings.cs                  # Task 7
│   │       ├── AppConfig.cs                    # Task 7
│   │       ├── CustomSymbol.cs                 # Task 7
│   │       ├── SymbolCatalog.cs                # Task 7 — merged built-in + custom lookup
│   │       └── ConfigStore.cs                  # Task 7 — atomic JSON persistence
│   └── SymbolCommander.App/
│       ├── SymbolCommander.App.csproj
│       ├── app.manifest                        # Task 1 — PerMonitorV2 DPI
│       ├── App.xaml / App.xaml.cs              # Task 8, rewired Task 12/13
│       ├── Tray/TrayIcon.cs                    # Task 8 — NotifyIcon wrapper, runtime icon
│       ├── Interop/
│       │   ├── NativeMethods.cs                # Task 9 — P/Invoke declarations
│       │   ├── NativeInput.cs                  # Task 9 — SendInput helpers (keys, right-click)
│       │   ├── MouseHook.cs                    # Task 9 — WH_MOUSE_LL
│       │   ├── KeyboardHook.cs                 # Task 9 — WH_KEYBOARD_LL
│       │   └── HookHost.cs                     # Task 9 — dedicated hook thread + watchdog
│       ├── Overlay/OverlayWindow.xaml/.cs      # Task 10 — trail, toast, red fade
│       ├── Execution/
│       │   ├── VkMap.cs                        # Task 11 — key name → virtual-key code
│       │   └── ActionExecutor.cs               # Task 11 — four action types
│       ├── Engine/GestureCoordinator.cs        # Task 12 — wires everything
│       ├── StartupManager.cs                   # Task 13 — HKCU Run key
│       └── Settings/
│           ├── SettingsWindow.xaml/.cs         # Task 13 — window + tabs shell
│           ├── GeneralTab.xaml/.cs             # Task 13 (+ actions list in Task 14)
│           ├── ActionEditorDialog.xaml/.cs     # Task 14
│           ├── KeystrokeRecorderBox.cs         # Task 14
│           ├── BindingsTab.xaml/.cs            # Task 15
│           ├── DrawingCanvas.cs                # Task 15 — shared stroke-capture control
│           ├── SymbolsTab.xaml/.cs             # Task 16
│           └── SymbolEditorDialog.xaml/.cs     # Task 16
└── tests/
    └── SymbolCommander.Core.Tests/
        ├── SymbolCommander.Core.Tests.csproj
        ├── StrokePreprocessorTests.cs          # Task 2
        ├── ProtractorRecognizerTests.cs        # Task 3
        ├── BuiltInSymbolsTests.cs              # Task 4
        ├── GestureEngineTests.cs               # Task 5
        ├── KeystrokeParserTests.cs             # Task 6
        ├── ActionValidatorTests.cs             # Task 6
        ├── ConfigStoreTests.cs                 # Task 7
        └── SymbolCatalogTests.cs               # Task 7
```

---

### Task 1: Toolchain + solution scaffold + cross-build smoke test

Installs the .NET 8 SDK on this Linux box, scaffolds the solution, and — critically — proves right now that a WPF app publishes for `win-x64` from Linux. If that smoke test fails, nothing else matters; stop and investigate before proceeding.

**Files:**
- Create: `SymbolCommander.sln`, `.gitignore`
- Create: `src/SymbolCommander.Core/SymbolCommander.Core.csproj` (via template)
- Create: `src/SymbolCommander.App/SymbolCommander.App.csproj` (via template, then replaced)
- Create: `src/SymbolCommander.App/app.manifest`
- Create: `tests/SymbolCommander.Core.Tests/SymbolCommander.Core.Tests.csproj` (via template)

**Interfaces:**
- Consumes: nothing.
- Produces: a building solution; `dotnet test` green; `SymbolCommander.exe` publishable via the exact publish command below (Task 17 reuses it).

- [ ] **Step 1: Install .NET 8 SDK**

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
grep -q 'DOTNET_ROOT' ~/.bashrc || printf '\nexport DOTNET_ROOT="$HOME/.dotnet"\nexport PATH="$HOME/.dotnet:$PATH"\n' >> ~/.bashrc
export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"
dotnet --version
```

Expected: prints an `8.0.x` version. (Install takes a few minutes.)

- [ ] **Step 2: Scaffold solution and projects**

```bash
cd /home/dev1/Rohith/symbol-commander
dotnet new sln -n SymbolCommander
dotnet new classlib -n SymbolCommander.Core -o src/SymbolCommander.Core -f net8.0
rm src/SymbolCommander.Core/Class1.cs
dotnet new xunit -n SymbolCommander.Core.Tests -o tests/SymbolCommander.Core.Tests -f net8.0
rm tests/SymbolCommander.Core.Tests/UnitTest1.cs
dotnet new wpf -n SymbolCommander.App -o src/SymbolCommander.App
dotnet sln add src/SymbolCommander.Core tests/SymbolCommander.Core.Tests src/SymbolCommander.App
dotnet add tests/SymbolCommander.Core.Tests reference src/SymbolCommander.Core
dotnet add src/SymbolCommander.App reference src/SymbolCommander.Core
```

Expected: each command reports success. (If `dotnet new wpf` rejects a `-f` flag you didn't pass, fine — we didn't pass one; SDK 8 generates `net8.0-windows`.)

- [ ] **Step 3: Replace the App csproj and add the DPI manifest**

Write `src/SymbolCommander.App/SymbolCommander.App.csproj` with exactly:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AssemblyName>SymbolCommander</AssemblyName>
    <RootNamespace>SymbolCommander.App</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\SymbolCommander.Core\SymbolCommander.Core.csproj" />
  </ItemGroup>
</Project>
```

Write `src/SymbolCommander.App/app.manifest` with exactly:

```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <application xmlns="urn:schemas-microsoft-com:asm.v3">
    <windowsSettings>
      <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
    </windowsSettings>
  </application>
</assembly>
```

Write `.gitignore` at repo root with exactly:

```
bin/
obj/
*.user
publish/
```

- [ ] **Step 4: Verify build, tests, and the win-x64 publish smoke test**

```bash
cd /home/dev1/Rohith/symbol-commander
dotnet build
dotnet test
dotnet publish src/SymbolCommander.App -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
ls -lh src/SymbolCommander.App/bin/Release/net8.0-windows/win-x64/publish/SymbolCommander.exe
```

Expected: build succeeds; test run reports 0 failed (0 tests is fine at this point); publish produces `SymbolCommander.exe` (~70–150 MB self-contained). If publish fails here, STOP — investigate `EnableWindowsTargeting` before any further task.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: scaffold solution, cross-build smoke test for win-x64

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Stroke preprocessing (GesturePoint + StrokePreprocessor)

**Files:**
- Create: `src/SymbolCommander.Core/Recognition/GesturePoint.cs`
- Create: `src/SymbolCommander.Core/Recognition/StrokePreprocessor.cs`
- Test: `tests/SymbolCommander.Core.Tests/StrokePreprocessorTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces (used by Tasks 3–5, 7, 15, 16):
  - `readonly record struct GesturePoint(double X, double Y)` in namespace `SymbolCommander.Core.Recognition`
  - `static class StrokePreprocessor` members:
    - `const int ResampleCount = 64`, `const double MinPathLength = 30.0`, `const int MinRawPoints = 5`
    - `double Distance(GesturePoint a, GesturePoint b)`
    - `double PathLength(IReadOnlyList<GesturePoint> pts)`
    - `bool IsValidStroke(IReadOnlyList<GesturePoint> raw)`
    - `GesturePoint[] Resample(IReadOnlyList<GesturePoint> pts, int n = ResampleCount)`
    - `GesturePoint[] Normalize(IReadOnlyList<GesturePoint> pts)` — scale so max bbox dimension = 1 (aspect preserved), centroid at origin
    - `double[] Vectorize(IReadOnlyList<GesturePoint> normalized)` — flatten to [x0,y0,x1,y1,…], magnitude-normalized to unit length

- [ ] **Step 1: Write the failing tests**

Write `tests/SymbolCommander.Core.Tests/StrokePreprocessorTests.cs`:

```csharp
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class StrokePreprocessorTests
{
    private static List<GesturePoint> Line(double x0, double y0, double x1, double y1, int n)
    {
        var pts = new List<GesturePoint>();
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)(n - 1);
            pts.Add(new GesturePoint(x0 + t * (x1 - x0), y0 + t * (y1 - y0)));
        }
        return pts;
    }

    [Fact]
    public void PathLength_of_straight_line_is_its_length()
    {
        var pts = Line(0, 0, 300, 0, 10);
        Assert.Equal(300.0, StrokePreprocessor.PathLength(pts), 6);
    }

    [Fact]
    public void IsValidStroke_rejects_too_few_points()
    {
        Assert.False(StrokePreprocessor.IsValidStroke(Line(0, 0, 300, 0, 4)));
    }

    [Fact]
    public void IsValidStroke_rejects_too_short_path()
    {
        Assert.False(StrokePreprocessor.IsValidStroke(Line(0, 0, 20, 0, 10)));
    }

    [Fact]
    public void IsValidStroke_accepts_long_enough_stroke()
    {
        Assert.True(StrokePreprocessor.IsValidStroke(Line(0, 0, 300, 0, 10)));
    }

    [Fact]
    public void Resample_returns_exactly_n_evenly_spaced_points()
    {
        var resampled = StrokePreprocessor.Resample(Line(0, 0, 630, 0, 7), 64);
        Assert.Equal(64, resampled.Length);
        double expectedGap = 630.0 / 63;
        for (int i = 1; i < resampled.Length; i++)
        {
            double gap = StrokePreprocessor.Distance(resampled[i - 1], resampled[i]);
            Assert.InRange(gap, expectedGap - 0.5, expectedGap + 0.5);
        }
        Assert.Equal(0, resampled[0].X, 6);
        Assert.Equal(630, resampled[^1].X, 3);
    }

    [Fact]
    public void Resample_survives_consecutive_duplicate_points()
    {
        var pts = Line(0, 0, 100, 0, 10);
        pts.Insert(5, pts[4]); // exact duplicate
        pts.Insert(5, pts[4]);
        var resampled = StrokePreprocessor.Resample(pts, 32);
        Assert.Equal(32, resampled.Length);
        Assert.All(resampled, p => Assert.False(double.IsNaN(p.X) || double.IsNaN(p.Y)));
    }

    [Fact]
    public void Normalize_centers_centroid_at_origin_and_scales_max_dimension_to_one()
    {
        var pts = StrokePreprocessor.Resample(Line(100, 200, 500, 300, 20), 64);
        var norm = StrokePreprocessor.Normalize(pts);
        double cx = norm.Average(p => p.X), cy = norm.Average(p => p.Y);
        Assert.Equal(0, cx, 6);
        Assert.Equal(0, cy, 6);
        double w = norm.Max(p => p.X) - norm.Min(p => p.X);
        double h = norm.Max(p => p.Y) - norm.Min(p => p.Y);
        Assert.Equal(1.0, Math.Max(w, h), 6);
        // aspect preserved: original was 400 wide x 100 tall
        Assert.Equal(0.25, Math.Min(w, h) / Math.Max(w, h), 2);
    }

    [Fact]
    public void Normalize_handles_degenerate_zero_size_stroke_without_NaN()
    {
        var pts = Enumerable.Repeat(new GesturePoint(50, 50), 10).ToList();
        var norm = StrokePreprocessor.Normalize(pts);
        Assert.All(norm, p => Assert.False(double.IsNaN(p.X) || double.IsNaN(p.Y)));
    }

    [Fact]
    public void Vectorize_produces_unit_magnitude_vector_of_2n_elements()
    {
        var norm = StrokePreprocessor.Normalize(StrokePreprocessor.Resample(Line(0, 0, 300, 150, 20), 64));
        var v = StrokePreprocessor.Vectorize(norm);
        Assert.Equal(128, v.Length);
        double mag = Math.Sqrt(v.Sum(x => x * x));
        Assert.Equal(1.0, mag, 6);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test 2>&1 | tail -5
```

Expected: compilation errors — `GesturePoint` / `StrokePreprocessor` do not exist. That counts as the failing state.

- [ ] **Step 3: Implement**

Write `src/SymbolCommander.Core/Recognition/GesturePoint.cs`:

```csharp
namespace SymbolCommander.Core.Recognition;

public readonly record struct GesturePoint(double X, double Y);
```

Write `src/SymbolCommander.Core/Recognition/StrokePreprocessor.cs`:

```csharp
namespace SymbolCommander.Core.Recognition;

public static class StrokePreprocessor
{
    public const int ResampleCount = 64;
    public const double MinPathLength = 30.0;
    public const int MinRawPoints = 5;

    public static double Distance(GesturePoint a, GesturePoint b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static double PathLength(IReadOnlyList<GesturePoint> pts)
    {
        double d = 0;
        for (int i = 1; i < pts.Count; i++) d += Distance(pts[i - 1], pts[i]);
        return d;
    }

    public static bool IsValidStroke(IReadOnlyList<GesturePoint> raw) =>
        raw.Count >= MinRawPoints && PathLength(raw) >= MinPathLength;

    public static GesturePoint[] Resample(IReadOnlyList<GesturePoint> pts, int n = ResampleCount)
    {
        // drop consecutive duplicates so zero-length segments can't divide by zero
        var src = new List<GesturePoint> { pts[0] };
        for (int i = 1; i < pts.Count; i++)
            if (pts[i] != src[^1]) src.Add(pts[i]);
        if (src.Count == 1) return Enumerable.Repeat(src[0], n).ToArray();

        double interval = PathLength(src) / (n - 1);
        var result = new List<GesturePoint>(n) { src[0] };
        double d = 0;
        for (int i = 1; i < src.Count; i++)
        {
            double seg = Distance(src[i - 1], src[i]);
            if (d + seg >= interval)
            {
                double t = (interval - d) / seg;
                var q = new GesturePoint(
                    src[i - 1].X + t * (src[i].X - src[i - 1].X),
                    src[i - 1].Y + t * (src[i].Y - src[i - 1].Y));
                result.Add(q);
                src.Insert(i, q);
                d = 0;
            }
            else d += seg;
        }
        while (result.Count < n) result.Add(src[^1]);
        return result.Take(n).ToArray();
    }

    public static GesturePoint[] Normalize(IReadOnlyList<GesturePoint> pts)
    {
        double minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X);
        double minY = pts.Min(p => p.Y), maxY = pts.Max(p => p.Y);
        double scale = Math.Max(maxX - minX, maxY - minY);
        if (scale < 1e-9) scale = 1;
        double cx = pts.Average(p => p.X), cy = pts.Average(p => p.Y);
        return pts.Select(p => new GesturePoint((p.X - cx) / scale, (p.Y - cy) / scale)).ToArray();
    }

    public static double[] Vectorize(IReadOnlyList<GesturePoint> normalized)
    {
        var v = new double[normalized.Count * 2];
        for (int i = 0; i < normalized.Count; i++)
        {
            v[i * 2] = normalized[i].X;
            v[i * 2 + 1] = normalized[i].Y;
        }
        double mag = Math.Sqrt(v.Sum(x => x * x));
        if (mag < 1e-12) return v;
        for (int i = 0; i < v.Length; i++) v[i] /= mag;
        return v;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: all tests pass, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): stroke preprocessing pipeline

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Protractor recognizer

**Files:**
- Create: `src/SymbolCommander.Core/Recognition/SymbolTemplate.cs`
- Create: `src/SymbolCommander.Core/Recognition/RecognitionResult.cs`
- Create: `src/SymbolCommander.Core/Recognition/ProtractorRecognizer.cs`
- Test: `tests/SymbolCommander.Core.Tests/ProtractorRecognizerTests.cs`

**Interfaces:**
- Consumes: `StrokePreprocessor`, `GesturePoint` (Task 2).
- Produces (used by Tasks 4, 7, 12, 15, 16):
  - `sealed record SymbolTemplate(string SymbolId, double[] Vector)`
  - `sealed record RecognitionResult(string? SymbolId, double Score)` with `bool IsMatch => SymbolId is not null`
  - `static class ProtractorRecognizer`:
    - `const double MaxRotationRadians = Math.PI / 6` (±30°)
    - `double[] ToVector(IReadOnlyList<GesturePoint> rawStroke)` — full preprocess pipeline
    - `double Similarity(double[] template, double[] candidate)` — clamped optimal-rotation cosine similarity, ≤ 1
    - `RecognitionResult Recognize(IReadOnlyList<GesturePoint> raw, IEnumerable<SymbolTemplate> templates, double threshold)`

- [ ] **Step 1: Write the failing tests**

Write `tests/SymbolCommander.Core.Tests/ProtractorRecognizerTests.cs`:

```csharp
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class ProtractorRecognizerTests
{
    // dense "V" stroke: down-right then up-right, 300px scale
    private static List<GesturePoint> VStroke()
    {
        var pts = new List<GesturePoint>();
        for (int i = 0; i <= 30; i++) pts.Add(new GesturePoint(i * 5, i * 10));
        for (int i = 0; i <= 30; i++) pts.Add(new GesturePoint(150 + i * 5, 300 - i * 10));
        return pts;
    }

    // dense circle stroke, clockwise from top
    private static List<GesturePoint> CircleStroke()
    {
        var pts = new List<GesturePoint>();
        for (int i = 0; i <= 64; i++)
        {
            double th = Math.PI / 2 - 2 * Math.PI * i / 64.0;
            pts.Add(new GesturePoint(200 + 150 * Math.Cos(th), 200 - 150 * Math.Sin(th)));
        }
        return pts;
    }

    private static List<GesturePoint> Rotated(List<GesturePoint> pts, double degrees)
    {
        double cx = pts.Average(p => p.X), cy = pts.Average(p => p.Y);
        double r = degrees * Math.PI / 180, cos = Math.Cos(r), sin = Math.Sin(r);
        return pts.Select(p => new GesturePoint(
            cx + (p.X - cx) * cos - (p.Y - cy) * sin,
            cy + (p.X - cx) * sin + (p.Y - cy) * cos)).ToList();
    }

    private static SymbolTemplate T(string id, List<GesturePoint> pts) =>
        new(id, ProtractorRecognizer.ToVector(pts));

    [Fact]
    public void Identical_stroke_matches_with_near_perfect_score()
    {
        var result = ProtractorRecognizer.Recognize(VStroke(), new[] { T("v", VStroke()) }, 0.80);
        Assert.True(result.IsMatch);
        Assert.Equal("v", result.SymbolId);
        Assert.True(result.Score > 0.99, $"score was {result.Score}");
    }

    [Fact]
    public void Slightly_rotated_stroke_still_matches()
    {
        var result = ProtractorRecognizer.Recognize(
            Rotated(VStroke(), 20), new[] { T("v", VStroke()) }, 0.80);
        Assert.True(result.IsMatch);
        Assert.True(result.Score > 0.95, $"score was {result.Score}");
    }

    [Fact]
    public void Heavily_rotated_stroke_does_not_match()
    {
        // V rotated 180° is a caret — must NOT match V (this is the M-vs-W guarantee)
        var result = ProtractorRecognizer.Recognize(
            Rotated(VStroke(), 180), new[] { T("v", VStroke()) }, 0.80);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Best_of_multiple_templates_wins()
    {
        var templates = new[] { T("v", VStroke()), T("circle", CircleStroke()) };
        var result = ProtractorRecognizer.Recognize(CircleStroke(), templates, 0.80);
        Assert.Equal("circle", result.SymbolId);
    }

    [Fact]
    public void Different_shape_scores_below_threshold()
    {
        var result = ProtractorRecognizer.Recognize(VStroke(), new[] { T("circle", CircleStroke()) }, 0.80);
        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Invalid_stroke_returns_no_match_score_zero()
    {
        var tiny = new List<GesturePoint> { new(0, 0), new(1, 1), new(2, 2) };
        var result = ProtractorRecognizer.Recognize(tiny, new[] { T("v", VStroke()) }, 0.80);
        Assert.False(result.IsMatch);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Empty_template_list_returns_no_match()
    {
        var result = ProtractorRecognizer.Recognize(VStroke(), Array.Empty<SymbolTemplate>(), 0.80);
        Assert.False(result.IsMatch);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: compilation errors (types don't exist).

- [ ] **Step 3: Implement**

Write `src/SymbolCommander.Core/Recognition/SymbolTemplate.cs`:

```csharp
namespace SymbolCommander.Core.Recognition;

public sealed record SymbolTemplate(string SymbolId, double[] Vector);
```

Write `src/SymbolCommander.Core/Recognition/RecognitionResult.cs`:

```csharp
namespace SymbolCommander.Core.Recognition;

public sealed record RecognitionResult(string? SymbolId, double Score)
{
    public bool IsMatch => SymbolId is not null;
    public static readonly RecognitionResult None = new((string?)null, 0);
}
```

Write `src/SymbolCommander.Core/Recognition/ProtractorRecognizer.cs`:

```csharp
namespace SymbolCommander.Core.Recognition;

/// <summary>
/// Protractor unistroke matcher (Li, CHI 2010): closed-form optimal-rotation cosine
/// similarity between magnitude-normalized point vectors. Orientation-sensitive:
/// the optimal rotation is clamped to ±30° so e.g. M and W (180° apart) stay distinct.
/// </summary>
public static class ProtractorRecognizer
{
    public const double MaxRotationRadians = Math.PI / 6;

    public static double[] ToVector(IReadOnlyList<GesturePoint> rawStroke) =>
        StrokePreprocessor.Vectorize(StrokePreprocessor.Normalize(StrokePreprocessor.Resample(rawStroke)));

    public static double Similarity(double[] template, double[] candidate)
    {
        double a = 0, b = 0;
        int len = Math.Min(template.Length, candidate.Length);
        for (int i = 0; i + 1 < len; i += 2)
        {
            a += template[i] * candidate[i] + template[i + 1] * candidate[i + 1];
            b += template[i] * candidate[i + 1] - template[i + 1] * candidate[i];
        }
        double angle = Math.Clamp(Math.Atan2(b, a), -MaxRotationRadians, MaxRotationRadians);
        return Math.Min(1.0, a * Math.Cos(angle) + b * Math.Sin(angle));
    }

    public static RecognitionResult Recognize(
        IReadOnlyList<GesturePoint> raw, IEnumerable<SymbolTemplate> templates, double threshold)
    {
        if (!StrokePreprocessor.IsValidStroke(raw)) return RecognitionResult.None;
        var g = ToVector(raw);
        string? bestId = null;
        double bestScore = double.MinValue;
        foreach (var t in templates)
        {
            double s = Similarity(t.Vector, g);
            if (s > bestScore) { bestScore = s; bestId = t.SymbolId; }
        }
        if (bestId is null) return RecognitionResult.None;
        return bestScore >= threshold
            ? new RecognitionResult(bestId, bestScore)
            : new RecognitionResult(null, Math.Max(0, bestScore));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: all tests pass, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): Protractor recognizer with clamped rotation

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Built-in symbol library

**Files:**
- Create: `src/SymbolCommander.Core/Library/BuiltInSymbols.cs`
- Test: `tests/SymbolCommander.Core.Tests/BuiltInSymbolsTests.cs`

**Interfaces:**
- Consumes: `GesturePoint`, `StrokePreprocessor`, `SymbolTemplate`, `ProtractorRecognizer` (Tasks 2–3).
- Produces (used by Tasks 7, 12, 15, 16):
  - `sealed record BuiltInSymbol(string Id, string Name, IReadOnlyList<GesturePoint[]> TemplateStrokes)`
  - `static class BuiltInSymbols` in namespace `SymbolCommander.Core.Library`:
    - `IReadOnlyList<BuiltInSymbol> All` — ids: `c, l, m, n, s, u, v, w, z, circle, triangle, check, caret, up, down, left, right`
    - `IReadOnlyList<SymbolTemplate> Templates` — one `SymbolTemplate` per template stroke (circle has two: CW + CCW)

- [ ] **Step 1: Write the failing tests**

Write `tests/SymbolCommander.Core.Tests/BuiltInSymbolsTests.cs`:

```csharp
using SymbolCommander.Core.Library;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class BuiltInSymbolsTests
{
    // Simulate a human drawing: scale to ~300px, offset, jitter ±4px (seeded), rotate 8°
    private static List<GesturePoint> Humanize(GesturePoint[] template, int seed)
    {
        var rng = new Random(seed);
        double rot = 8 * Math.PI / 180, cos = Math.Cos(rot), sin = Math.Sin(rot);
        return template.Select(p =>
        {
            double x = p.X * 300 + 100 + (rng.NextDouble() - 0.5) * 8;
            double y = p.Y * 300 + 100 + (rng.NextDouble() - 0.5) * 8;
            return new GesturePoint(
                150 + (x - 150) * cos - (y - 150) * sin,
                150 + (x - 150) * sin + (y - 150) * cos);
        }).ToList();
    }

    [Fact]
    public void Library_has_expected_symbols()
    {
        var ids = BuiltInSymbols.All.Select(s => s.Id).ToHashSet();
        var expected = new[] { "c", "l", "m", "n", "s", "u", "v", "w", "z",
            "circle", "triangle", "check", "caret", "up", "down", "left", "right" };
        Assert.Equal(expected.Length, ids.Count);
        Assert.All(expected, id => Assert.Contains(id, ids));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Every_symbol_recognizes_a_humanized_drawing_of_itself(int seed)
    {
        foreach (var sym in BuiltInSymbols.All)
        {
            var drawn = Humanize(sym.TemplateStrokes[0], seed * 31 + sym.Id.Length);
            var result = ProtractorRecognizer.Recognize(drawn, BuiltInSymbols.Templates, 0.80);
            Assert.True(result.IsMatch, $"{sym.Id} (seed {seed}) did not match anything, score {result.Score:F3}");
            Assert.True(sym.Id == result.SymbolId,
                $"{sym.Id} (seed {seed}) recognized as {result.SymbolId} ({result.Score:F3})");
        }
    }

    [Fact]
    public void M_and_W_are_distinct()
    {
        var m = BuiltInSymbols.All.First(s => s.Id == "m");
        var result = ProtractorRecognizer.Recognize(
            Humanize(m.TemplateStrokes[0], 7), BuiltInSymbols.Templates, 0.80);
        Assert.Equal("m", result.SymbolId);
    }

    [Fact]
    public void Counterclockwise_circle_also_recognizes_as_circle()
    {
        var circle = BuiltInSymbols.All.First(s => s.Id == "circle");
        Assert.True(circle.TemplateStrokes.Count >= 2);
        var drawn = Humanize(circle.TemplateStrokes[1], 11);
        var result = ProtractorRecognizer.Recognize(drawn, BuiltInSymbols.Templates, 0.80);
        Assert.Equal("circle", result.SymbolId);
    }

    [Fact]
    public void Every_template_stroke_has_at_least_32_points()
    {
        foreach (var sym in BuiltInSymbols.All)
            Assert.All(sym.TemplateStrokes, s => Assert.True(s.Length >= 32));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: compilation errors (`BuiltInSymbols` doesn't exist).

- [ ] **Step 3: Implement**

Write `src/SymbolCommander.Core/Library/BuiltInSymbols.cs`. Coordinates are in a unit square, **Y grows downward** (screen convention), in natural drawing order. `Densify` reuses `Resample` to turn sparse vertices into smooth 64-point strokes.

```csharp
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Library;

public sealed record BuiltInSymbol(string Id, string Name, IReadOnlyList<GesturePoint[]> TemplateStrokes);

public static class BuiltInSymbols
{
    public static IReadOnlyList<BuiltInSymbol> All { get; }
    public static IReadOnlyList<SymbolTemplate> Templates { get; }

    static BuiltInSymbols()
    {
        static GesturePoint[] Poly(params (double x, double y)[] v) =>
            StrokePreprocessor.Resample(v.Select(p => new GesturePoint(p.x, p.y)).ToList(), 64);

        // point(θ): screen coords, θ=90° points visually up (y decreases)
        static GesturePoint[] Arc(double cx, double cy, double r, double startDeg, double endDeg)
        {
            var pts = new List<GesturePoint>();
            for (int i = 0; i <= 63; i++)
            {
                double th = (startDeg + (endDeg - startDeg) * i / 63.0) * Math.PI / 180;
                pts.Add(new GesturePoint(cx + r * Math.Cos(th), cy - r * Math.Sin(th)));
            }
            return pts.ToArray();
        }

        All = new List<BuiltInSymbol>
        {
            new("c", "C", new[] { Arc(0.5, 0.5, 0.45, 60, 300) }),
            new("l", "L", new[] { Poly((0, 0), (0, 1), (1, 1)) }),
            new("m", "M", new[] { Poly((0, 1), (0, 0), (0.5, 0.6), (1, 0), (1, 1)) }),
            new("n", "N", new[] { Poly((0, 1), (0, 0), (1, 1), (1, 0)) }),
            new("s", "S", new[] { Poly((0.75, 0.05), (0.4, 0.05), (0.25, 0.2), (0.35, 0.42),
                                       (0.65, 0.58), (0.75, 0.8), (0.6, 0.95), (0.25, 0.95)) }),
            new("u", "U", new[] { Poly((0, 0), (0, 0.6), (0.15, 0.9), (0.5, 1),
                                       (0.85, 0.9), (1, 0.6), (1, 0)) }),
            new("v", "V", new[] { Poly((0, 0), (0.5, 1), (1, 0)) }),
            new("w", "W", new[] { Poly((0, 0), (0.25, 1), (0.5, 0.4), (0.75, 1), (1, 0)) }),
            new("z", "Z", new[] { Poly((0, 0), (1, 0), (0, 1), (1, 1)) }),
            new("circle", "Circle", new[]
            {
                Arc(0.5, 0.5, 0.45, 90, -270),  // clockwise from top
                Arc(0.5, 0.5, 0.45, 90, 450),   // counterclockwise from top
            }),
            new("triangle", "Triangle", new[] { Poly((0.5, 0), (0, 1), (1, 1), (0.5, 0)) }),
            new("check", "Check", new[] { Poly((0, 0.55), (0.35, 1), (1, 0)) }),
            new("caret", "Caret", new[] { Poly((0, 1), (0.5, 0), (1, 1)) }),
            new("up", "Up", new[] { Poly((0.5, 1), (0.5, 0)) }),
            new("down", "Down", new[] { Poly((0.5, 0), (0.5, 1)) }),
            new("left", "Left", new[] { Poly((1, 0.5), (0, 0.5)) }),
            new("right", "Right", new[] { Poly((0, 0.5), (1, 0.5)) }),
        };

        Templates = All
            .SelectMany(s => s.TemplateStrokes.Select(st =>
                new SymbolTemplate(s.Id, ProtractorRecognizer.ToVector(st))))
            .ToList();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: all tests pass, 0 failed.

**If a `Every_symbol_recognizes...` case fails:** the test output names the confused pair and scores. Fix by adjusting that symbol's vertex list (make the shapes more distinct — e.g. deepen M's middle dip, sharpen S's curves), NOT by lowering the 0.80 threshold or widening the rotation clamp. Re-run until green.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): built-in symbol library (17 symbols)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Gesture engine state machine

The heart of the trigger/passthrough logic, deliberately in Core (no Windows types) so it's fully unit-tested here. The App feeds it trigger/pointer events from hooks; it emits events the App maps to overlay drawing, recognition, and right-click replay.

**Files:**
- Create: `src/SymbolCommander.Core/Engine/TriggerSource.cs`
- Create: `src/SymbolCommander.Core/Engine/GestureEngine.cs`
- Test: `tests/SymbolCommander.Core.Tests/GestureEngineTests.cs`

**Interfaces:**
- Consumes: `GesturePoint` (Task 2).
- Produces (used by Task 12):
  - `enum TriggerSource { RightButton, Hotkey }` in namespace `SymbolCommander.Core.Engine`
  - `enum EngineState { Idle, Pending, Drawing }`
  - `sealed class GestureEngine`:
    - `double MoveThresholdPx { get; set; } = 10`
    - `EngineState State { get; }`, `TriggerSource? ActiveSource { get; }`
    - `void TriggerDown(GesturePoint p, TriggerSource source)` — ignored if not Idle
    - `void PointerMoved(GesturePoint p)` — ignored if Idle
    - `void TriggerUp(GesturePoint p)` — ignored if Idle
    - `void Cancel()` — Escape pressed; returns to Idle silently (subsequent TriggerUp is a no-op)
    - events: `Action<GesturePoint>? TrailStarted`, `Action<GesturePoint>? TrailPointAdded`,
      `Action<IReadOnlyList<GesturePoint>>? StrokeCompleted`,
      `Action<GesturePoint, TriggerSource>? ClickPassthroughRequested`, `Action? Cancelled`

- [ ] **Step 1: Write the failing tests**

Write `tests/SymbolCommander.Core.Tests/GestureEngineTests.cs`:

```csharp
using SymbolCommander.Core.Engine;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class GestureEngineTests
{
    private readonly GestureEngine _e = new();
    private readonly List<string> _events = new();
    private IReadOnlyList<GesturePoint>? _stroke;
    private (GesturePoint p, TriggerSource s)? _passthrough;

    public GestureEngineTests()
    {
        _e.TrailStarted += p => _events.Add("start");
        _e.TrailPointAdded += p => _events.Add("point");
        _e.StrokeCompleted += s => { _events.Add("stroke"); _stroke = s; };
        _e.ClickPassthroughRequested += (p, s) => { _events.Add("passthrough"); _passthrough = (p, s); };
        _e.Cancelled += () => _events.Add("cancelled");
    }

    [Fact]
    public void Quick_release_without_movement_requests_click_passthrough()
    {
        _e.TriggerDown(new(100, 100), TriggerSource.RightButton);
        _e.PointerMoved(new(103, 102)); // under 10px threshold
        _e.TriggerUp(new(103, 102));
        Assert.Equal(EngineState.Idle, _e.State);
        Assert.Equal(new GesturePoint(103, 102), _passthrough!.Value.p);
        Assert.Equal(TriggerSource.RightButton, _passthrough.Value.s);
        Assert.DoesNotContain("stroke", _events);
        Assert.DoesNotContain("start", _events);
    }

    [Fact]
    public void Movement_past_threshold_enters_drawing_and_completes_stroke()
    {
        _e.TriggerDown(new(100, 100), TriggerSource.RightButton);
        Assert.Equal(EngineState.Pending, _e.State);
        _e.PointerMoved(new(105, 100));
        Assert.Equal(EngineState.Pending, _e.State);
        _e.PointerMoved(new(115, 100)); // 15px from start → Drawing
        Assert.Equal(EngineState.Drawing, _e.State);
        _e.PointerMoved(new(130, 110));
        _e.TriggerUp(new(140, 120));
        Assert.Equal(EngineState.Idle, _e.State);
        Assert.Contains("start", _events);
        Assert.Contains("stroke", _events);
        Assert.DoesNotContain("passthrough", _events);
        // stroke contains every point from trigger-down through trigger-up
        Assert.Equal(new GesturePoint(100, 100), _stroke![0]);
        Assert.Equal(new GesturePoint(140, 120), _stroke[^1]);
        Assert.Equal(5, _stroke.Count);
    }

    [Fact]
    public void ActiveSource_reflects_current_session()
    {
        Assert.Null(_e.ActiveSource);
        _e.TriggerDown(new(0, 0), TriggerSource.Hotkey);
        Assert.Equal(TriggerSource.Hotkey, _e.ActiveSource);
        _e.TriggerUp(new(0, 0));
        Assert.Null(_e.ActiveSource);
    }

    [Fact]
    public void Hotkey_quick_release_reports_hotkey_source_in_passthrough()
    {
        _e.TriggerDown(new(50, 50), TriggerSource.Hotkey);
        _e.TriggerUp(new(50, 50));
        Assert.Equal(TriggerSource.Hotkey, _passthrough!.Value.s);
    }

    [Fact]
    public void Cancel_while_drawing_raises_cancelled_and_swallows_the_following_trigger_up()
    {
        _e.TriggerDown(new(0, 0), TriggerSource.RightButton);
        _e.PointerMoved(new(50, 50));
        Assert.Equal(EngineState.Drawing, _e.State);
        _e.Cancel();
        Assert.Equal(EngineState.Idle, _e.State);
        Assert.Contains("cancelled", _events);
        _e.TriggerUp(new(50, 50)); // physical button release after Escape
        Assert.DoesNotContain("stroke", _events);
        Assert.DoesNotContain("passthrough", _events);
    }

    [Fact]
    public void Cancel_when_idle_does_nothing()
    {
        _e.Cancel();
        Assert.Empty(_events);
    }

    [Fact]
    public void TriggerDown_while_active_is_ignored()
    {
        _e.TriggerDown(new(0, 0), TriggerSource.RightButton);
        _e.PointerMoved(new(50, 50));
        _e.TriggerDown(new(200, 200), TriggerSource.Hotkey); // ignored
        Assert.Equal(TriggerSource.RightButton, _e.ActiveSource);
        _e.TriggerUp(new(60, 60));
        Assert.Single(_events.Where(e => e == "stroke"));
    }

    [Fact]
    public void Trail_events_fire_for_every_drawing_point()
    {
        _e.TriggerDown(new(0, 0), TriggerSource.RightButton);
        _e.PointerMoved(new(20, 0));  // start (past threshold)
        _e.PointerMoved(new(40, 0));  // point
        _e.PointerMoved(new(60, 0));  // point
        Assert.Equal(1, _events.Count(e => e == "start"));
        Assert.True(_events.Count(e => e == "point") >= 2);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test 2>&1 | tail -5
```

Expected: compilation errors.

- [ ] **Step 3: Implement**

Write `src/SymbolCommander.Core/Engine/TriggerSource.cs`:

```csharp
namespace SymbolCommander.Core.Engine;

public enum TriggerSource { RightButton, Hotkey }

public enum EngineState { Idle, Pending, Drawing }
```

Write `src/SymbolCommander.Core/Engine/GestureEngine.cs`:

```csharp
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Engine;

/// <summary>
/// Trigger/stroke state machine. Thread-agnostic: the caller must feed events
/// from a single thread (the hook thread). Pending → Drawing happens once the
/// pointer moves MoveThresholdPx from the trigger-down point; a release while
/// still Pending is a plain click and requests passthrough instead.
/// </summary>
public sealed class GestureEngine
{
    public double MoveThresholdPx { get; set; } = 10;

    public EngineState State { get; private set; } = EngineState.Idle;
    public TriggerSource? ActiveSource { get; private set; }

    public event Action<GesturePoint>? TrailStarted;
    public event Action<GesturePoint>? TrailPointAdded;
    public event Action<IReadOnlyList<GesturePoint>>? StrokeCompleted;
    public event Action<GesturePoint, TriggerSource>? ClickPassthroughRequested;
    public event Action? Cancelled;

    private readonly List<GesturePoint> _points = new();
    private GesturePoint _start;

    public void TriggerDown(GesturePoint p, TriggerSource source)
    {
        if (State != EngineState.Idle) return;
        State = EngineState.Pending;
        ActiveSource = source;
        _start = p;
        _points.Clear();
        _points.Add(p);
    }

    public void PointerMoved(GesturePoint p)
    {
        switch (State)
        {
            case EngineState.Pending:
                _points.Add(p);
                if (StrokePreprocessor.Distance(_start, p) >= MoveThresholdPx)
                {
                    State = EngineState.Drawing;
                    TrailStarted?.Invoke(_start);
                    foreach (var pt in _points.Skip(1)) TrailPointAdded?.Invoke(pt);
                }
                break;
            case EngineState.Drawing:
                _points.Add(p);
                TrailPointAdded?.Invoke(p);
                break;
        }
    }

    public void TriggerUp(GesturePoint p)
    {
        var source = ActiveSource;
        switch (State)
        {
            case EngineState.Pending:
                Reset();
                ClickPassthroughRequested?.Invoke(p, source!.Value);
                break;
            case EngineState.Drawing:
                _points.Add(p);
                var stroke = _points.ToArray();
                Reset();
                StrokeCompleted?.Invoke(stroke);
                break;
        }
    }

    public void Cancel()
    {
        if (State == EngineState.Idle) return;
        Reset();
        Cancelled?.Invoke();
    }

    private void Reset()
    {
        State = EngineState.Idle;
        ActiveSource = null;
        _points.Clear();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: all tests pass, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): gesture trigger state machine with click passthrough

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Action model + keystroke parser + validator

**Files:**
- Create: `src/SymbolCommander.Core/Actions/ActionType.cs`
- Create: `src/SymbolCommander.Core/Actions/WindowMediaCommand.cs`
- Create: `src/SymbolCommander.Core/Actions/ActionDefinition.cs`
- Create: `src/SymbolCommander.Core/Actions/Binding.cs`
- Create: `src/SymbolCommander.Core/Actions/KeystrokeParser.cs`
- Create: `src/SymbolCommander.Core/Actions/ActionValidator.cs`
- Test: `tests/SymbolCommander.Core.Tests/KeystrokeParserTests.cs`
- Test: `tests/SymbolCommander.Core.Tests/ActionValidatorTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces (used by Tasks 7, 11, 12, 14, 15):
  - `enum ActionType { Keystroke, Launch, WindowMedia, Shell }` in namespace `SymbolCommander.Core.Actions`
  - `enum WindowMediaCommand { MinimizeWindow, MaximizeWindow, RestoreWindow, CloseWindow, SnapLeft, SnapRight, ShowDesktop, VolumeUp, VolumeDown, VolumeMute, MediaPlayPause, MediaNext, MediaPrevious, LockWorkstation }`
  - `sealed class ActionDefinition { string Id; string Name; ActionType Type; Dictionary<string,string> Parameters }`
    — parameter keys by type: Keystroke: `keys`; Launch: `target`; WindowMedia: `command` (enum name); Shell: `commandLine`, `hidden` ("true"/"false")
  - `sealed class Binding { string SymbolId; string ActionId; bool Enabled }`
  - `sealed record KeyCombo(IReadOnlyList<string> Modifiers, string Key)` — modifiers normalized to `Ctrl|Alt|Shift|Win`, key normalized to canonical casing
  - `static class KeystrokeParser`:
    - `IReadOnlyList<string> KnownKeys` — A–Z, 0–9, F1–F12, Enter, Tab, Space, Esc, Backspace, Delete, Insert, Home, End, PageUp, PageDown, Up, Down, Left, Right, PrintScreen
    - `IReadOnlyList<KeyCombo> Parse(string spec)` — `"Ctrl+Shift+T, Ctrl+K"` → 2 combos; throws `FormatException` with a human message on anything invalid
  - `static class ActionValidator`: `string? Validate(ActionDefinition a)` — null when valid, else error message

- [ ] **Step 1: Write the failing tests**

Write `tests/SymbolCommander.Core.Tests/KeystrokeParserTests.cs`:

```csharp
using SymbolCommander.Core.Actions;

namespace SymbolCommander.Core.Tests;

public class KeystrokeParserTests
{
    [Fact]
    public void Parses_single_combo()
    {
        var combos = KeystrokeParser.Parse("Ctrl+W");
        var c = Assert.Single(combos);
        Assert.Equal(new[] { "Ctrl" }, c.Modifiers);
        Assert.Equal("W", c.Key);
    }

    [Fact]
    public void Parses_sequence_of_combos()
    {
        var combos = KeystrokeParser.Parse("Ctrl+K, Ctrl+S");
        Assert.Equal(2, combos.Count);
        Assert.Equal("K", combos[0].Key);
        Assert.Equal("S", combos[1].Key);
    }

    [Fact]
    public void Is_case_insensitive_and_trims_whitespace()
    {
        var c = Assert.Single(KeystrokeParser.Parse("  ctrl + shift + t "));
        Assert.Equal(new[] { "Ctrl", "Shift" }, c.Modifiers);
        Assert.Equal("T", c.Key);
    }

    [Fact]
    public void Bare_key_without_modifiers_is_valid()
    {
        var c = Assert.Single(KeystrokeParser.Parse("F5"));
        Assert.Empty(c.Modifiers);
        Assert.Equal("F5", c.Key);
    }

    [Fact]
    public void Named_keys_normalize_casing()
    {
        Assert.Equal("PageDown", KeystrokeParser.Parse("ctrl+pagedown")[0].Key);
        Assert.Equal("Enter", KeystrokeParser.Parse("ENTER")[0].Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Foo+X")]
    [InlineData("Ctrl+NotAKey")]
    [InlineData("Ctrl+Shift")]  // modifier alone is not a key
    public void Invalid_specs_throw_FormatException(string spec)
    {
        Assert.Throws<FormatException>(() => KeystrokeParser.Parse(spec));
    }

    [Fact]
    public void Win_modifier_is_supported()
    {
        var c = Assert.Single(KeystrokeParser.Parse("Win+D"));
        Assert.Equal(new[] { "Win" }, c.Modifiers);
        Assert.Equal("D", c.Key);
    }
}
```

Write `tests/SymbolCommander.Core.Tests/ActionValidatorTests.cs`:

```csharp
using SymbolCommander.Core.Actions;

namespace SymbolCommander.Core.Tests;

public class ActionValidatorTests
{
    private static ActionDefinition Make(ActionType type, params (string k, string v)[] ps) => new()
    {
        Name = "test",
        Type = type,
        Parameters = ps.ToDictionary(p => p.k, p => p.v),
    };

    [Fact]
    public void Valid_keystroke_passes()
        => Assert.Null(ActionValidator.Validate(Make(ActionType.Keystroke, ("keys", "Ctrl+W"))));

    [Fact]
    public void Keystroke_with_bad_spec_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.Keystroke, ("keys", "Nope+X"))));

    [Fact]
    public void Keystroke_missing_parameter_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.Keystroke)));

    [Fact]
    public void Valid_launch_passes()
        => Assert.Null(ActionValidator.Validate(Make(ActionType.Launch, ("target", "https://example.com"))));

    [Fact]
    public void Launch_with_empty_target_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.Launch, ("target", "  "))));

    [Fact]
    public void Valid_window_media_passes()
        => Assert.Null(ActionValidator.Validate(Make(ActionType.WindowMedia, ("command", "VolumeUp"))));

    [Fact]
    public void Unknown_window_media_command_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.WindowMedia, ("command", "Reboot"))));

    [Fact]
    public void Valid_shell_passes()
        => Assert.Null(ActionValidator.Validate(Make(ActionType.Shell, ("commandLine", "echo hi"))));

    [Fact]
    public void Shell_with_empty_command_fails()
        => Assert.NotNull(ActionValidator.Validate(Make(ActionType.Shell, ("commandLine", ""))));

    [Fact]
    public void Blank_name_fails()
    {
        var a = Make(ActionType.Launch, ("target", "x"));
        a.Name = "";
        Assert.NotNull(ActionValidator.Validate(a));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: compilation errors.

- [ ] **Step 3: Implement**

Write `src/SymbolCommander.Core/Actions/ActionType.cs`:

```csharp
namespace SymbolCommander.Core.Actions;

public enum ActionType { Keystroke, Launch, WindowMedia, Shell }
```

Write `src/SymbolCommander.Core/Actions/WindowMediaCommand.cs`:

```csharp
namespace SymbolCommander.Core.Actions;

public enum WindowMediaCommand
{
    MinimizeWindow, MaximizeWindow, RestoreWindow, CloseWindow,
    SnapLeft, SnapRight, ShowDesktop,
    VolumeUp, VolumeDown, VolumeMute,
    MediaPlayPause, MediaNext, MediaPrevious,
    LockWorkstation,
}
```

Write `src/SymbolCommander.Core/Actions/ActionDefinition.cs`:

```csharp
namespace SymbolCommander.Core.Actions;

public sealed class ActionDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public ActionType Type { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();

    public string Get(string key) => Parameters.TryGetValue(key, out var v) ? v : "";
}
```

Write `src/SymbolCommander.Core/Actions/Binding.cs`:

```csharp
namespace SymbolCommander.Core.Actions;

public sealed class Binding
{
    public string SymbolId { get; set; } = "";
    public string ActionId { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
```

Write `src/SymbolCommander.Core/Actions/KeystrokeParser.cs`:

```csharp
namespace SymbolCommander.Core.Actions;

public sealed record KeyCombo(IReadOnlyList<string> Modifiers, string Key);

public static class KeystrokeParser
{
    private static readonly string[] ModifierNames = { "Ctrl", "Alt", "Shift", "Win" };

    public static IReadOnlyList<string> KnownKeys { get; } = BuildKnownKeys();

    private static string[] BuildKnownKeys()
    {
        var keys = new List<string>();
        for (char c = 'A'; c <= 'Z'; c++) keys.Add(c.ToString());
        for (char c = '0'; c <= '9'; c++) keys.Add(c.ToString());
        for (int i = 1; i <= 12; i++) keys.Add($"F{i}");
        keys.AddRange(new[] { "Enter", "Tab", "Space", "Esc", "Backspace", "Delete", "Insert",
            "Home", "End", "PageUp", "PageDown", "Up", "Down", "Left", "Right", "PrintScreen" });
        return keys.ToArray();
    }

    public static IReadOnlyList<KeyCombo> Parse(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
            throw new FormatException("Keystroke is empty.");

        var combos = new List<KeyCombo>();
        foreach (var part in spec.Split(','))
        {
            var tokens = part.Split('+').Select(t => t.Trim()).ToArray();
            if (tokens.Any(t => t.Length == 0))
                throw new FormatException($"Malformed combo: \"{part.Trim()}\"");

            var mods = new List<string>();
            for (int i = 0; i < tokens.Length - 1; i++)
            {
                var m = ModifierNames.FirstOrDefault(n => n.Equals(tokens[i], StringComparison.OrdinalIgnoreCase))
                    ?? throw new FormatException($"Unknown modifier \"{tokens[i]}\" (use Ctrl, Alt, Shift, Win).");
                if (!mods.Contains(m)) mods.Add(m);
            }

            var last = tokens[^1];
            var key = KnownKeys.FirstOrDefault(k => k.Equals(last, StringComparison.OrdinalIgnoreCase))
                ?? throw new FormatException($"Unknown key \"{last}\".");
            combos.Add(new KeyCombo(mods, key));
        }
        return combos;
    }
}
```

Write `src/SymbolCommander.Core/Actions/ActionValidator.cs`:

```csharp
namespace SymbolCommander.Core.Actions;

public static class ActionValidator
{
    /// <returns>null when valid; otherwise a human-readable error.</returns>
    public static string? Validate(ActionDefinition a)
    {
        if (string.IsNullOrWhiteSpace(a.Name)) return "Action needs a name.";
        switch (a.Type)
        {
            case ActionType.Keystroke:
                try { KeystrokeParser.Parse(a.Get("keys")); }
                catch (FormatException ex) { return ex.Message; }
                return null;
            case ActionType.Launch:
                return string.IsNullOrWhiteSpace(a.Get("target"))
                    ? "Launch action needs a program, file, folder, or URL." : null;
            case ActionType.WindowMedia:
                return Enum.TryParse<WindowMediaCommand>(a.Get("command"), out _)
                    ? null : $"Unknown window/media command \"{a.Get("command")}\".";
            case ActionType.Shell:
                return string.IsNullOrWhiteSpace(a.Get("commandLine"))
                    ? "Shell action needs a command line." : null;
            default:
                return "Unknown action type.";
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: all tests pass, 0 failed.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): action model, keystroke parser, validator

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Config store, defaults, symbol catalog

**Files:**
- Create: `src/SymbolCommander.Core/Config/AppSettings.cs`
- Create: `src/SymbolCommander.Core/Config/AppConfig.cs`
- Create: `src/SymbolCommander.Core/Config/CustomSymbol.cs`
- Create: `src/SymbolCommander.Core/Config/SymbolCatalog.cs`
- Create: `src/SymbolCommander.Core/Config/ConfigStore.cs`
- Test: `tests/SymbolCommander.Core.Tests/ConfigStoreTests.cs`
- Test: `tests/SymbolCommander.Core.Tests/SymbolCatalogTests.cs`

**Interfaces:**
- Consumes: Tasks 2–4, 6 types.
- Produces (used by Tasks 8, 12–16):
  - `sealed class AppSettings` — defaults in parens: `bool GesturesEnabled` (true), `bool RightButtonTriggerEnabled` (true), `bool HotkeyTriggerEnabled` (true), `string HotkeyModifiers` ("Ctrl+Alt"), `double Sensitivity` (0.80), `string TrailColor` ("#3399FF"), `double TrailThickness` (4.0), `bool StartWithWindows` (false)
  - `sealed class AppConfig { int SchemaVersion = 1; AppSettings Settings; List<ActionDefinition> Actions; List<Binding> Bindings; AppConfig Clone() }`
  - `sealed class CustomSymbol { string Id; string Name; List<List<GesturePoint>> Examples }`
  - `sealed class SymbolCatalog` — ctor `(IEnumerable<CustomSymbol> customs)`:
    - `IReadOnlyList<(string Id, string Name, bool IsBuiltIn)> All`
    - `string? NameOf(string symbolId)`
    - `IReadOnlyList<SymbolTemplate> AllTemplates`
    - `IReadOnlyList<SymbolTemplate> TemplatesFor(IEnumerable<string> symbolIds)`
  - `sealed class ConfigStore` — ctor `(string directory)`:
    - `string ConfigPath`, `event Action<string>? LoadWarning`
    - `AppConfig Load()`, `void Save(AppConfig config)`
    - `List<CustomSymbol> LoadCustomSymbols()`, `void SaveCustomSymbol(CustomSymbol s)`, `void DeleteCustomSymbol(string id)`
    - `static AppConfig DefaultConfig()`

- [ ] **Step 1: Write the failing tests**

Write `tests/SymbolCommander.Core.Tests/ConfigStoreTests.cs`:

```csharp
using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Library;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sc-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Fact]
    public void Load_with_no_file_returns_defaults()
    {
        var store = new ConfigStore(_dir);
        var config = store.Load();
        Assert.Equal(1, config.SchemaVersion);
        Assert.True(config.Settings.GesturesEnabled);
        Assert.NotEmpty(config.Actions);
        Assert.NotEmpty(config.Bindings);
    }

    [Fact]
    public void Save_then_load_round_trips_everything()
    {
        var store = new ConfigStore(_dir);
        var config = ConfigStore.DefaultConfig();
        config.Settings.Sensitivity = 0.66;
        config.Settings.HotkeyModifiers = "Ctrl+Shift";
        config.Actions.Add(new ActionDefinition
        {
            Name = "Notepad",
            Type = ActionType.Launch,
            Parameters = { ["target"] = @"C:\Windows\notepad.exe" },
        });
        store.Save(config);

        var loaded = new ConfigStore(_dir).Load();
        Assert.Equal(0.66, loaded.Settings.Sensitivity);
        Assert.Equal("Ctrl+Shift", loaded.Settings.HotkeyModifiers);
        Assert.Contains(loaded.Actions, a => a.Name == "Notepad" && a.Type == ActionType.Launch
            && a.Get("target") == @"C:\Windows\notepad.exe");
        Assert.Equal(config.Bindings.Count, loaded.Bindings.Count);
    }

    [Fact]
    public void Save_is_atomic_no_tmp_file_left_behind()
    {
        var store = new ConfigStore(_dir);
        store.Save(ConfigStore.DefaultConfig());
        Assert.True(File.Exists(store.ConfigPath));
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public void Corrupt_config_backs_up_warns_and_returns_defaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "config.json"), "{ this is not json !!");
        var store = new ConfigStore(_dir);
        string? warning = null;
        store.LoadWarning += w => warning = w;

        var config = store.Load();

        Assert.NotNull(warning);
        Assert.NotEmpty(config.Actions); // defaults
        Assert.NotEmpty(Directory.GetFiles(_dir, "config.json.corrupt-*"));
    }

    [Fact]
    public void Custom_symbols_save_load_delete()
    {
        var store = new ConfigStore(_dir);
        var sym = new CustomSymbol
        {
            Name = "Star",
            Examples = { new List<GesturePoint> { new(0, 0), new(10, 10), new(20, 0) } },
        };
        store.SaveCustomSymbol(sym);

        var loaded = new ConfigStore(_dir).LoadCustomSymbols();
        var s = Assert.Single(loaded);
        Assert.Equal("Star", s.Name);
        Assert.Equal(sym.Id, s.Id);
        Assert.Equal(new GesturePoint(10, 10), s.Examples[0][1]);

        store.DeleteCustomSymbol(sym.Id);
        Assert.Empty(new ConfigStore(_dir).LoadCustomSymbols());
    }

    [Fact]
    public void Default_config_is_internally_consistent()
    {
        var config = ConfigStore.DefaultConfig();
        var actionIds = config.Actions.Select(a => a.Id).ToHashSet();
        var builtInIds = BuiltInSymbols.All.Select(s => s.Id).ToHashSet();
        foreach (var b in config.Bindings)
        {
            Assert.Contains(b.ActionId, actionIds);
            Assert.Contains(b.SymbolId, builtInIds);
        }
        foreach (var a in config.Actions)
            Assert.Null(ActionValidator.Validate(a));
    }

    [Fact]
    public void Clone_is_deep()
    {
        var config = ConfigStore.DefaultConfig();
        var clone = config.Clone();
        clone.Settings.Sensitivity = 0.11;
        clone.Actions[0].Name = "mutated";
        Assert.NotEqual(0.11, config.Settings.Sensitivity);
        Assert.NotEqual("mutated", config.Actions[0].Name);
    }
}
```

Write `tests/SymbolCommander.Core.Tests/SymbolCatalogTests.cs`:

```csharp
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Library;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Tests;

public class SymbolCatalogTests
{
    private static CustomSymbol Star()
    {
        var pts = new List<GesturePoint>();
        for (int i = 0; i <= 40; i++) pts.Add(new GesturePoint(i * 5, (i % 2) * 100));
        return new CustomSymbol { Name = "Star", Examples = { pts, pts, pts } };
    }

    [Fact]
    public void Catalog_merges_built_ins_and_customs()
    {
        var star = Star();
        var catalog = new SymbolCatalog(new[] { star });
        Assert.Equal(BuiltInSymbols.All.Count + 1, catalog.All.Count);
        Assert.Equal("Star", catalog.NameOf(star.Id));
        Assert.Equal("M", catalog.NameOf("m"));
        Assert.Null(catalog.NameOf("nonexistent"));
        Assert.Contains(catalog.All, e => e.Id == star.Id && !e.IsBuiltIn);
    }

    [Fact]
    public void Custom_symbol_contributes_one_template_per_example()
    {
        var star = Star();
        var catalog = new SymbolCatalog(new[] { star });
        Assert.Equal(3, catalog.AllTemplates.Count(t => t.SymbolId == star.Id));
    }

    [Fact]
    public void TemplatesFor_filters_by_symbol_id()
    {
        var catalog = new SymbolCatalog(Array.Empty<CustomSymbol>());
        var templates = catalog.TemplatesFor(new[] { "m", "circle" });
        Assert.All(templates, t => Assert.True(t.SymbolId is "m" or "circle"));
        Assert.Equal(3, templates.Count); // m has 1 template, circle has 2
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: compilation errors.

- [ ] **Step 3: Implement**

Write `src/SymbolCommander.Core/Config/AppSettings.cs`:

```csharp
namespace SymbolCommander.Core.Config;

public sealed class AppSettings
{
    public bool GesturesEnabled { get; set; } = true;
    public bool RightButtonTriggerEnabled { get; set; } = true;
    public bool HotkeyTriggerEnabled { get; set; } = true;
    public string HotkeyModifiers { get; set; } = "Ctrl+Alt";
    public double Sensitivity { get; set; } = 0.80;
    public string TrailColor { get; set; } = "#3399FF";
    public double TrailThickness { get; set; } = 4.0;
    public bool StartWithWindows { get; set; }
}
```

Write `src/SymbolCommander.Core/Config/AppConfig.cs`:

```csharp
using System.Text.Json;
using SymbolCommander.Core.Actions;

namespace SymbolCommander.Core.Config;

public sealed class AppConfig
{
    public int SchemaVersion { get; set; } = 1;
    public AppSettings Settings { get; set; } = new();
    public List<ActionDefinition> Actions { get; set; } = new();
    public List<Binding> Bindings { get; set; } = new();

    public AppConfig Clone() =>
        JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(this, ConfigStore.JsonOptions), ConfigStore.JsonOptions)!;
}
```

Write `src/SymbolCommander.Core/Config/CustomSymbol.cs`:

```csharp
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Config;

public sealed class CustomSymbol
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public List<List<GesturePoint>> Examples { get; set; } = new();
}
```

Write `src/SymbolCommander.Core/Config/SymbolCatalog.cs`:

```csharp
using SymbolCommander.Core.Library;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.Core.Config;

/// <summary>Immutable merged view of built-in and custom symbols. Rebuild after config changes.</summary>
public sealed class SymbolCatalog
{
    private readonly Dictionary<string, string> _names = new();

    public IReadOnlyList<(string Id, string Name, bool IsBuiltIn)> All { get; }
    public IReadOnlyList<SymbolTemplate> AllTemplates { get; }

    public SymbolCatalog(IEnumerable<CustomSymbol> customs)
    {
        var all = new List<(string, string, bool)>();
        var templates = new List<SymbolTemplate>(BuiltInSymbols.Templates);

        foreach (var s in BuiltInSymbols.All)
        {
            all.Add((s.Id, s.Name, true));
            _names[s.Id] = s.Name;
        }
        foreach (var c in customs)
        {
            all.Add((c.Id, c.Name, false));
            _names[c.Id] = c.Name;
            templates.AddRange(c.Examples.Select(e =>
                new SymbolTemplate(c.Id, ProtractorRecognizer.ToVector(e))));
        }
        All = all;
        AllTemplates = templates;
    }

    public string? NameOf(string symbolId) => _names.TryGetValue(symbolId, out var n) ? n : null;

    public IReadOnlyList<SymbolTemplate> TemplatesFor(IEnumerable<string> symbolIds)
    {
        var wanted = symbolIds.ToHashSet();
        return AllTemplates.Where(t => wanted.Contains(t.SymbolId)).ToList();
    }
}
```

Write `src/SymbolCommander.Core/Config/ConfigStore.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using SymbolCommander.Core.Actions;

namespace SymbolCommander.Core.Config;

public sealed class ConfigStore
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _dir;
    private string SymbolsDir => Path.Combine(_dir, "symbols");

    public string ConfigPath => Path.Combine(_dir, "config.json");
    public event Action<string>? LoadWarning;

    public ConfigStore(string directory) => _dir = directory;

    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath)) return DefaultConfig();
        try
        {
            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), JsonOptions);
            return config ?? DefaultConfig();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            var backup = ConfigPath + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            try { File.Copy(ConfigPath, backup, overwrite: true); } catch (IOException) { }
            LoadWarning?.Invoke($"Config file was unreadable and has been reset. Backup: {backup}");
            return DefaultConfig();
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(_dir);
        var tmp = ConfigPath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, JsonOptions));
        File.Move(tmp, ConfigPath, overwrite: true);
    }

    public List<CustomSymbol> LoadCustomSymbols()
    {
        var result = new List<CustomSymbol>();
        if (!Directory.Exists(SymbolsDir)) return result;
        foreach (var file in Directory.GetFiles(SymbolsDir, "*.json"))
        {
            try
            {
                var sym = JsonSerializer.Deserialize<CustomSymbol>(File.ReadAllText(file), JsonOptions);
                if (sym is not null) result.Add(sym);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                LoadWarning?.Invoke($"Skipped unreadable symbol file {Path.GetFileName(file)}.");
            }
        }
        return result;
    }

    public void SaveCustomSymbol(CustomSymbol s)
    {
        Directory.CreateDirectory(SymbolsDir);
        var path = Path.Combine(SymbolsDir, s.Id + ".json");
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(s, JsonOptions));
        File.Move(tmp, path, overwrite: true);
    }

    public void DeleteCustomSymbol(string id)
    {
        var path = Path.Combine(SymbolsDir, id + ".json");
        if (File.Exists(path)) File.Delete(path);
    }

    public static AppConfig DefaultConfig()
    {
        var browser = new ActionDefinition { Name = "Open browser", Type = ActionType.Launch,
            Parameters = { ["target"] = "https://www.google.com" } };
        var minimize = new ActionDefinition { Name = "Minimize window", Type = ActionType.WindowMedia,
            Parameters = { ["command"] = nameof(WindowMediaCommand.MinimizeWindow) } };
        var undo = new ActionDefinition { Name = "Undo", Type = ActionType.Keystroke,
            Parameters = { ["keys"] = "Ctrl+Z" } };
        var volUp = new ActionDefinition { Name = "Volume up", Type = ActionType.WindowMedia,
            Parameters = { ["command"] = nameof(WindowMediaCommand.VolumeUp) } };
        var volDown = new ActionDefinition { Name = "Volume down", Type = ActionType.WindowMedia,
            Parameters = { ["command"] = nameof(WindowMediaCommand.VolumeDown) } };
        var back = new ActionDefinition { Name = "Back", Type = ActionType.Keystroke,
            Parameters = { ["keys"] = "Alt+Left" } };
        var forward = new ActionDefinition { Name = "Forward", Type = ActionType.Keystroke,
            Parameters = { ["keys"] = "Alt+Right" } };
        var showDesktop = new ActionDefinition { Name = "Show desktop", Type = ActionType.WindowMedia,
            Parameters = { ["command"] = nameof(WindowMediaCommand.ShowDesktop) } };

        return new AppConfig
        {
            Actions = { browser, minimize, undo, volUp, volDown, back, forward, showDesktop },
            Bindings =
            {
                new Binding { SymbolId = "w", ActionId = browser.Id },
                new Binding { SymbolId = "m", ActionId = minimize.Id },
                new Binding { SymbolId = "z", ActionId = undo.Id },
                new Binding { SymbolId = "up", ActionId = volUp.Id },
                new Binding { SymbolId = "down", ActionId = volDown.Id },
                new Binding { SymbolId = "left", ActionId = back.Id },
                new Binding { SymbolId = "right", ActionId = forward.Id },
                new Binding { SymbolId = "circle", ActionId = showDesktop.Id },
            },
        };
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet test 2>&1 | tail -5
```

Expected: all tests pass, 0 failed. This completes spec milestone 1 — the whole Core is done and tested.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(core): config store, default bindings, symbol catalog

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 8: Tray application shell + single instance

From here through Task 12 the code is Windows-only: verification on Linux is `dotnet build` (compile) only. Follow the code exactly; runtime verification happens at the Task 12 milestone checklist on the Windows PC.

**Files:**
- Delete: `src/SymbolCommander.App/MainWindow.xaml`, `src/SymbolCommander.App/MainWindow.xaml.cs`
- Modify: `src/SymbolCommander.App/App.xaml`, `src/SymbolCommander.App/App.xaml.cs` (full replacements below)
- Create: `src/SymbolCommander.App/Tray/TrayIcon.cs`

**Interfaces:**
- Consumes: `ConfigStore`, `AppConfig` (Task 7).
- Produces (used by Tasks 12, 13):
  - `sealed class TrayIcon : IDisposable` — ctor `()`; properties/events:
    - `event Action? SettingsRequested`, `event Action? ExitRequested`, `event Action<bool>? GesturesToggled`
    - `void SetGesturesEnabled(bool on)` (updates the check mark)
    - `void ShowNotification(string title, string message, bool warning = false)`
  - `App` exposes `public static new App Current`, `public ConfigStore ConfigStore`, `public TrayIcon Tray` and raises settings/exit wiring (extended in Tasks 12–13).

- [ ] **Step 1: Delete the template main window and replace App.xaml**

```bash
rm src/SymbolCommander.App/MainWindow.xaml src/SymbolCommander.App/MainWindow.xaml.cs
```

Write `src/SymbolCommander.App/App.xaml`:

```xml
<Application x:Class="SymbolCommander.App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources />
</Application>
```

- [ ] **Step 2: Write TrayIcon**

Write `src/SymbolCommander.App/Tray/TrayIcon.cs`:

```csharp
using System.Drawing;
using System.Windows.Forms;

namespace SymbolCommander.App.Tray;

/// <summary>WinForms NotifyIcon wrapper with a runtime-drawn icon (no binary assets).</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _enabledItem;

    public event Action? SettingsRequested;
    public event Action? ExitRequested;
    public event Action<bool>? GesturesToggled;

    public TrayIcon()
    {
        _enabledItem = new ToolStripMenuItem("Gestures enabled") { Checked = true, CheckOnClick = true };
        _enabledItem.CheckedChanged += (_, _) => GesturesToggled?.Invoke(_enabledItem.Checked);

        var settings = new ToolStripMenuItem("Settings…");
        settings.Click += (_, _) => SettingsRequested?.Invoke();

        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) => ExitRequested?.Invoke();

        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
            { _enabledItem, new ToolStripSeparator(), settings, new ToolStripSeparator(), exit });

        _icon = new NotifyIcon
        {
            Icon = DrawIcon(),
            Text = "Symbol Commander",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => SettingsRequested?.Invoke();
    }

    public void SetGesturesEnabled(bool on) => _enabledItem.Checked = on;

    public void ShowNotification(string title, string message, bool warning = false) =>
        _icon.ShowBalloonTip(3000, title, message, warning ? ToolTipIcon.Warning : ToolTipIcon.Info);

    private static Icon DrawIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        using var brush = new SolidBrush(Color.FromArgb(0x33, 0x99, 0xFF));
        g.FillEllipse(brush, 1, 1, 30, 30);
        using var font = new Font("Segoe UI", 16, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
        var size = g.MeasureString("S", font);
        g.DrawString("S", font, Brushes.White, (32 - size.Width) / 2, (32 - size.Height) / 2);
        // the icon handle must outlive the bitmap; Clone detaches it
        return (Icon)Icon.FromHandle(bmp.GetHicon()).Clone();
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
```

- [ ] **Step 3: Write App.xaml.cs (lifecycle + single instance)**

Write `src/SymbolCommander.App/App.xaml.cs`:

```csharp
using System.IO;
using System.Threading;
using System.Windows;
using SymbolCommander.App.Tray;
using SymbolCommander.Core.Config;

namespace SymbolCommander.App;

public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\SymbolCommander.SingleInstance";
    private const string ShowSettingsEventName = @"Local\SymbolCommander.ShowSettings";

    private Mutex? _instanceMutex;
    private EventWaitHandle? _showSettingsEvent;

    public static new App Current => (App)System.Windows.Application.Current;
    public ConfigStore ConfigStore { get; private set; } = null!;
    public TrayIcon Tray { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirst);
        _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        if (!isFirst)
        {
            // another instance is running: ask it to open settings, then quit
            _showSettingsEvent.Set();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SymbolCommander");
        ConfigStore = new ConfigStore(configDir);

        Tray = new TrayIcon();
        Tray.ExitRequested += Shutdown;
        Tray.SettingsRequested += OpenSettings;

        // wake up when a second instance signals us
        var waiter = new Thread(() =>
        {
            while (_showSettingsEvent.WaitOne())
                Dispatcher.BeginInvoke(OpenSettings);
        }) { IsBackground = true };
        waiter.Start();
    }

    private void OpenSettings()
    {
        // Task 13 replaces this with the real settings window
        Tray.ShowNotification("Symbol Commander", "Settings UI arrives in a later task.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Tray?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 4: Build**

```bash
dotnet build 2>&1 | tail -3
```

Expected: `Build succeeded. 0 Error(s)` (warnings acceptable).

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat(app): tray icon shell with single-instance guard

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

*Optional early Windows check (recommended if the PC is handy): publish (Task 1 Step 4 command), copy the exe over, run it — a blue "S" tray icon appears; Exit works; launching twice doesn't duplicate the icon.*

---

### Task 9: Low-level hooks + native input

**Files:**
- Create: `src/SymbolCommander.App/Interop/NativeMethods.cs`
- Create: `src/SymbolCommander.App/Interop/NativeInput.cs`
- Create: `src/SymbolCommander.App/Interop/MouseHook.cs`
- Create: `src/SymbolCommander.App/Interop/KeyboardHook.cs`
- Create: `src/SymbolCommander.App/Interop/HookHost.cs`

**Interfaces:**
- Consumes: nothing from Core.
- Produces (used by Tasks 11, 12):
  - `sealed class MouseHookEventArgs : EventArgs { int Message; int X; int Y; bool Injected; bool Suppress }` — messages: `MouseHook.WM_MOUSEMOVE/WM_RBUTTONDOWN/WM_RBUTTONUP` constants
  - `sealed class KeyboardHookEventArgs : EventArgs { uint VkCode; bool IsDown; bool Injected; bool Suppress }`
  - `sealed class MouseHook / KeyboardHook : IDisposable` — `void Install()`, `void Uninstall()`, `event EventHandler<…>`; handlers run ON THE HOOK THREAD and must be fast
  - `sealed class HookHost : IDisposable` — ctor `(MouseHook, KeyboardHook, Func<bool> reinstallAllowed)`; `void Start()` spins a background thread with a Dispatcher loop, installs both hooks there, re-installs both every 60 s when `reinstallAllowed()` (watchdog); `void Stop()`
  - `static class NativeInput` — `void SendCombo(IReadOnlyList<ushort> modifierVks, ushort keyVk)`, `void SendRightClick()` (at current cursor position, marked injected so hooks skip it), `(int X, int Y) CursorPos()`
  - `static class NativeMethods` — P/Invoke + `GetForegroundWindow`, `ShowWindowAsync`, `PostMessageW`, `LockWorkStation` for Task 11

- [ ] **Step 1: Write NativeMethods**

Write `src/SymbolCommander.App/Interop/NativeMethods.cs`:

```csharp
using System.Runtime.InteropServices;

namespace SymbolCommander.App.Interop;

internal static class NativeMethods
{
    internal delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    internal const int WH_KEYBOARD_LL = 13;
    internal const int WH_MOUSE_LL = 14;

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;   // bit 0 = LLMHF_INJECTED
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;   // bit 4 (0x10) = LLKHF_INJECTED
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public uint type;    // 0 = mouse, 1 = keyboard
        public INPUTUNION U;
    }

    internal const uint INPUT_MOUSE = 0;
    internal const uint INPUT_KEYBOARD = 1;
    internal const uint KEYEVENTF_KEYUP = 0x0002;
    internal const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    internal const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    internal const int SW_MINIMIZE = 6;
    internal const int SW_MAXIMIZE = 3;
    internal const int SW_RESTORE = 9;
    internal const uint WM_SYSCOMMAND = 0x0112;
    internal const nuint SC_CLOSE = 0xF060;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetWindowsHookExW(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    internal static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern IntPtr GetModuleHandleW(string? lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    internal static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    internal static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern bool PostMessageW(IntPtr hWnd, uint msg, nuint wParam, nint lParam);

    [DllImport("user32.dll")]
    internal static extern bool LockWorkStation();
}
```

- [ ] **Step 2: Write NativeInput**

Write `src/SymbolCommander.App/Interop/NativeInput.cs`:

```csharp
namespace SymbolCommander.App.Interop;

using static NativeMethods;

/// <summary>SendInput helpers. Everything sent here carries the injected flag,
/// which our own hooks check and skip — no feedback loops.</summary>
public static class NativeInput
{
    public static (int X, int Y) CursorPos()
    {
        NativeMethods.GetCursorPos(out var p);
        return (p.X, p.Y);
    }

    public static void SendCombo(IReadOnlyList<ushort> modifierVks, ushort keyVk)
    {
        var inputs = new List<INPUT>();
        foreach (var m in modifierVks) inputs.Add(Key(m, down: true));
        inputs.Add(Key(keyVk, down: true));
        inputs.Add(Key(keyVk, down: false));
        for (int i = modifierVks.Count - 1; i >= 0; i--) inputs.Add(Key(modifierVks[i], down: false));
        Send(inputs.ToArray());
    }

    public static void SendRightClick()
    {
        Send(new[]
        {
            new INPUT { type = INPUT_MOUSE, U = new INPUTUNION { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTDOWN } } },
            new INPUT { type = INPUT_MOUSE, U = new INPUTUNION { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_RIGHTUP } } },
        });
    }

    private static INPUT Key(ushort vk, bool down) => new()
    {
        type = INPUT_KEYBOARD,
        U = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = down ? 0u : KEYEVENTF_KEYUP } },
    };

    private static void Send(INPUT[] inputs) =>
        SendInput((uint)inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf<INPUT>());
}
```

- [ ] **Step 3: Write MouseHook and KeyboardHook**

Write `src/SymbolCommander.App/Interop/MouseHook.cs`:

```csharp
using System.Runtime.InteropServices;

namespace SymbolCommander.App.Interop;

using static NativeMethods;

public sealed class MouseHookEventArgs : EventArgs
{
    public required int Message { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    public required bool Injected { get; init; }
    public bool Suppress { get; set; }
}

/// <summary>WH_MOUSE_LL wrapper. Install/Uninstall must be called on a thread
/// that pumps messages (HookHost provides one). Keep handlers FAST — Windows
/// silently removes hooks whose callbacks stall.</summary>
public sealed class MouseHook : IDisposable
{
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;

    public event EventHandler<MouseHookEventArgs>? MouseEvent;

    private IntPtr _handle;
    private HookProc? _proc; // field keeps the delegate alive; a local would be GC'd under us

    public void Install()
    {
        if (_handle != IntPtr.Zero) return;
        _proc = Callback;
        _handle = SetWindowsHookExW(WH_MOUSE_LL, _proc, GetModuleHandleW(null), 0);
        if (_handle == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "SetWindowsHookEx(mouse) failed");
    }

    public void Uninstall()
    {
        if (_handle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_handle);
        _handle = IntPtr.Zero;
        _proc = null;
    }

    private IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && MouseEvent is not null)
        {
            int msg = (int)wParam;
            if (msg is WM_MOUSEMOVE or WM_RBUTTONDOWN or WM_RBUTTONUP)
            {
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var args = new MouseHookEventArgs
                {
                    Message = msg, X = data.pt.X, Y = data.pt.Y,
                    Injected = (data.flags & 0x1) != 0,
                };
                MouseEvent(this, args);
                if (args.Suppress) return 1;
            }
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
```

Write `src/SymbolCommander.App/Interop/KeyboardHook.cs`:

```csharp
using System.Runtime.InteropServices;

namespace SymbolCommander.App.Interop;

using static NativeMethods;

public sealed class KeyboardHookEventArgs : EventArgs
{
    public required uint VkCode { get; init; }
    public required bool IsDown { get; init; }
    public required bool Injected { get; init; }
    public bool Suppress { get; set; }
}

public sealed class KeyboardHook : IDisposable
{
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    public event EventHandler<KeyboardHookEventArgs>? KeyEvent;

    private IntPtr _handle;
    private HookProc? _proc;

    public void Install()
    {
        if (_handle != IntPtr.Zero) return;
        _proc = Callback;
        _handle = SetWindowsHookExW(WH_KEYBOARD_LL, _proc, GetModuleHandleW(null), 0);
        if (_handle == IntPtr.Zero)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "SetWindowsHookEx(keyboard) failed");
    }

    public void Uninstall()
    {
        if (_handle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_handle);
        _handle = IntPtr.Zero;
        _proc = null;
    }

    private IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && KeyEvent is not null)
        {
            int msg = (int)wParam;
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var args = new KeyboardHookEventArgs
            {
                VkCode = data.vkCode,
                IsDown = msg is WM_KEYDOWN or WM_SYSKEYDOWN,
                Injected = (data.flags & 0x10) != 0,
            };
            KeyEvent(this, args);
            if (args.Suppress) return 1;
        }
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}
```

- [ ] **Step 4: Write HookHost**

Write `src/SymbolCommander.App/Interop/HookHost.cs`:

```csharp
using System.Windows.Threading;

namespace SymbolCommander.App.Interop;

/// <summary>Runs both hooks on a dedicated message-pumping thread. A 60s watchdog
/// re-installs them (Windows silently drops hooks it considers slow); reinstall is
/// skipped while a gesture is in progress via the reinstallAllowed callback.</summary>
public sealed class HookHost : IDisposable
{
    private readonly MouseHook _mouse;
    private readonly KeyboardHook _keyboard;
    private readonly Func<bool> _reinstallAllowed;
    private Dispatcher? _dispatcher;
    private Thread? _thread;

    public HookHost(MouseHook mouse, KeyboardHook keyboard, Func<bool> reinstallAllowed)
    {
        _mouse = mouse;
        _keyboard = keyboard;
        _reinstallAllowed = reinstallAllowed;
    }

    public void Start()
    {
        if (_thread is not null) return;
        var ready = new ManualResetEventSlim();
        _thread = new Thread(() =>
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _mouse.Install();
            _keyboard.Install();

            var watchdog = new DispatcherTimer(TimeSpan.FromSeconds(60), DispatcherPriority.Normal, (_, _) =>
            {
                if (!_reinstallAllowed()) return;
                _mouse.Uninstall(); _mouse.Install();
                _keyboard.Uninstall(); _keyboard.Install();
            }, _dispatcher);
            watchdog.Start();

            ready.Set();
            Dispatcher.Run();

            _mouse.Uninstall();
            _keyboard.Uninstall();
        }) { IsBackground = true, Name = "SymbolCommander.Hooks" };
        _thread.Start();
        ready.Wait(TimeSpan.FromSeconds(5));
    }

    public void Stop()
    {
        // cross-thread: InvokeShutdown() would throw; BeginInvokeShutdown queues it
        _dispatcher?.BeginInvokeShutdown(DispatcherPriority.Normal);
        _thread?.Join(TimeSpan.FromSeconds(2));
        _thread = null;
        _dispatcher = null;
    }

    public void Dispose() => Stop();
}
```

- [ ] **Step 5: Build and commit**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet build 2>&1 | tail -3
git add -A && git commit -m "feat(app): low-level mouse/keyboard hooks with watchdog, SendInput helpers

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: `Build succeeded. 0 Error(s)`.

---

### Task 10: Overlay window (ink trail, toast, red fade)

**Files:**
- Create: `src/SymbolCommander.App/Overlay/OverlayWindow.xaml`
- Create: `src/SymbolCommander.App/Overlay/OverlayWindow.xaml.cs`

**Interfaces:**
- Consumes: nothing from Core.
- Produces (used by Task 12) — ALL methods must be called on the WPF UI thread:
  - `partial class OverlayWindow : Window` — ctor `()`; call `.Show()` once at startup (stays open, fully transparent and click-through)
  - `void ConfigureTrail(string colorHex, double thickness)`
  - `void StartTrail(double screenX, double screenY)` — physical screen px; converts internally
  - `void AddTrailPoint(double screenX, double screenY)`
  - `void EndTrailRecognized(string toastText)` — clears trail, shows fading toast near last point
  - `void EndTrailRejected()` — trail turns red and fades out
  - `void CancelTrail()` — clears immediately

- [ ] **Step 1: Write the XAML**

Write `src/SymbolCommander.App/Overlay/OverlayWindow.xaml`:

```xml
<Window x:Class="SymbolCommander.App.Overlay.OverlayWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        WindowStyle="None" AllowsTransparency="True" Background="Transparent"
        Topmost="True" ShowInTaskbar="False" ShowActivated="False"
        IsHitTestVisible="False" Focusable="False">
    <Canvas x:Name="RootCanvas">
        <Polyline x:Name="Trail" StrokeLineJoin="Round" StrokeStartLineCap="Round"
                  StrokeEndLineCap="Round" Visibility="Collapsed" />
        <Border x:Name="Toast" Visibility="Collapsed" Background="#DD222222" CornerRadius="6"
                Padding="12,6">
            <TextBlock x:Name="ToastText" Foreground="White" FontSize="16" FontWeight="SemiBold" />
        </Border>
    </Canvas>
</Window>
```

- [ ] **Step 2: Write the code-behind**

Write `src/SymbolCommander.App/Overlay/OverlayWindow.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SymbolCommander.App.Overlay;

public partial class OverlayWindow : Window
{
    private Brush _trailBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF));
    private Point _lastPoint;

    public OverlayWindow()
    {
        InitializeComponent();
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        Trail.StrokeThickness = 4.0;
        SourceInitialized += (_, _) => MakeClickThrough();
    }

    private void MakeClickThrough()
    {
        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public void ConfigureTrail(string colorHex, double thickness)
    {
        try { _trailBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)); }
        catch (FormatException) { _trailBrush = Brushes.DodgerBlue; }
        Trail.StrokeThickness = thickness;
    }

    private Point ToCanvas(double screenX, double screenY)
    {
        // physical px → DIPs relative to this window (handles DPI scaling)
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null) return new Point(screenX - Left, screenY - Top);
        var p = source.CompositionTarget.TransformFromDevice.Transform(new Point(screenX, screenY));
        // TransformFromDevice gives DIPs in screen space; window origin is at virtual screen origin (DIPs)
        return new Point(p.X - Left, p.Y - Top);
    }

    public void StartTrail(double screenX, double screenY)
    {
        Trail.BeginAnimation(OpacityProperty, null);
        Trail.Points = new PointCollection { ToCanvas(screenX, screenY) };
        Trail.Stroke = _trailBrush;
        Trail.Opacity = 1;
        Trail.Visibility = Visibility.Visible;
        Toast.Visibility = Visibility.Collapsed;
    }

    public void AddTrailPoint(double screenX, double screenY)
    {
        _lastPoint = ToCanvas(screenX, screenY);
        Trail.Points.Add(_lastPoint);
    }

    public void EndTrailRecognized(string toastText)
    {
        ClearTrail();
        ToastText.Text = toastText;
        Toast.Visibility = Visibility.Visible;
        Canvas.SetLeft(Toast, Math.Max(0, _lastPoint.X + 16));
        Canvas.SetTop(Toast, Math.Max(0, _lastPoint.Y + 16));
        Toast.Opacity = 1;
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(1200)) { BeginTime = TimeSpan.FromMilliseconds(500) };
        fade.Completed += (_, _) => Toast.Visibility = Visibility.Collapsed;
        Toast.BeginAnimation(OpacityProperty, fade);
    }

    public void EndTrailRejected()
    {
        Trail.Stroke = Brushes.IndianRed;
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(600));
        fade.Completed += (_, _) => ClearTrail();
        Trail.BeginAnimation(OpacityProperty, fade);
    }

    public void CancelTrail() => ClearTrail();

    private void ClearTrail()
    {
        Trail.BeginAnimation(OpacityProperty, null);
        Trail.Visibility = Visibility.Collapsed;
        Trail.Points = new PointCollection();
        Trail.Opacity = 1;
    }
}
```

(`using System.Windows.Controls;` is required — `Canvas.SetLeft/SetTop` live there.)

- [ ] **Step 3: Build and commit**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet build 2>&1 | tail -3
git add -A && git commit -m "feat(app): click-through overlay with ink trail, toast, red fade

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: `Build succeeded. 0 Error(s)`.

---

### Task 11: Action executor

**Files:**
- Create: `src/SymbolCommander.App/Execution/VkMap.cs`
- Create: `src/SymbolCommander.App/Execution/ActionExecutor.cs`

**Interfaces:**
- Consumes: `ActionDefinition`, `ActionType`, `WindowMediaCommand`, `KeystrokeParser`, `KeyCombo` (Task 6); `NativeInput`, `NativeMethods` (Task 9).
- Produces (used by Task 12):
  - `static class VkMap` — `ushort KeyVk(string keyName)` (throws `ArgumentException` on unknown), `ushort ModifierVk(string modifierName)`
  - `sealed class ActionExecutor` — `event Action<string>? ActionFailed`; `void Execute(ActionDefinition action)` — never throws; failures raise `ActionFailed` with a human message
- Design note: volume/media commands are sent as media virtual keys via `SendInput` (supersedes the spec's `APPCOMMAND` mention — same user-visible behavior, one mechanism for everything). Snap = simulated Win+Left/Right, ShowDesktop = Win+D, per spec.

- [ ] **Step 1: Write VkMap**

Write `src/SymbolCommander.App/Execution/VkMap.cs`:

```csharp
namespace SymbolCommander.App.Execution;

/// <summary>Key name (KeystrokeParser.KnownKeys) → Windows virtual-key code.</summary>
public static class VkMap
{
    private static readonly Dictionary<string, ushort> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = 0x0D, ["Tab"] = 0x09, ["Space"] = 0x20, ["Esc"] = 0x1B, // every KeystrokeParser.KnownKeys entry must resolve here
        ["Backspace"] = 0x08, ["Delete"] = 0x2E, ["Insert"] = 0x2D,
        ["Home"] = 0x24, ["End"] = 0x23, ["PageUp"] = 0x21, ["PageDown"] = 0x22,
        ["Up"] = 0x26, ["Down"] = 0x28, ["Left"] = 0x25, ["Right"] = 0x27,
        ["PrintScreen"] = 0x2C,
    };

    public static ushort KeyVk(string keyName)
    {
        if (keyName.Length == 1 && keyName[0] is (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
            return (ushort)keyName[0];
        if (keyName.Length is 2 or 3 && keyName[0] is 'F' or 'f'
            && int.TryParse(keyName[1..], out int f) && f is >= 1 and <= 12)
            return (ushort)(0x70 + f - 1);
        if (Named.TryGetValue(keyName, out var vk)) return vk;
        throw new ArgumentException($"No virtual key for \"{keyName}\".");
    }

    public static ushort ModifierVk(string modifierName) => modifierName switch
    {
        "Ctrl" => 0x11,  // VK_CONTROL
        "Alt" => 0x12,   // VK_MENU
        "Shift" => 0x10, // VK_SHIFT
        "Win" => 0x5B,   // VK_LWIN
        _ => throw new ArgumentException($"No virtual key for modifier \"{modifierName}\"."),
    };

    // media/volume VKs used by ActionExecutor
    public const ushort VK_VOLUME_MUTE = 0xAD;
    public const ushort VK_VOLUME_DOWN = 0xAE;
    public const ushort VK_VOLUME_UP = 0xAF;
    public const ushort VK_MEDIA_NEXT = 0xB0;
    public const ushort VK_MEDIA_PREV = 0xB1;
    public const ushort VK_MEDIA_PLAY_PAUSE = 0xB3;
}
```

- [ ] **Step 2: Write ActionExecutor**

Write `src/SymbolCommander.App/Execution/ActionExecutor.cs`:

```csharp
using System.Diagnostics;
using SymbolCommander.App.Interop;
using SymbolCommander.Core.Actions;

namespace SymbolCommander.App.Execution;

public sealed class ActionExecutor
{
    public event Action<string>? ActionFailed;

    public void Execute(ActionDefinition action)
    {
        try
        {
            switch (action.Type)
            {
                case ActionType.Keystroke: ExecuteKeystroke(action); break;
                case ActionType.Launch: ExecuteLaunch(action); break;
                case ActionType.WindowMedia: ExecuteWindowMedia(action); break;
                case ActionType.Shell: ExecuteShell(action); break;
            }
        }
        catch (Exception ex)
        {
            ActionFailed?.Invoke($"{action.Name}: {ex.Message}");
        }
    }

    private static void ExecuteKeystroke(ActionDefinition a)
    {
        foreach (var combo in KeystrokeParser.Parse(a.Get("keys")))
            SendCombo(combo);
    }

    private static void SendCombo(KeyCombo combo) =>
        NativeInput.SendCombo(combo.Modifiers.Select(VkMap.ModifierVk).ToArray(), VkMap.KeyVk(combo.Key));

    private static void ExecuteLaunch(ActionDefinition a) =>
        Process.Start(new ProcessStartInfo(a.Get("target")) { UseShellExecute = true });

    private static void ExecuteShell(ActionDefinition a)
    {
        bool hidden = !string.Equals(a.Get("hidden"), "false", StringComparison.OrdinalIgnoreCase);
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c {a.Get("commandLine")}")
        {
            UseShellExecute = false,
            CreateNoWindow = hidden,
        });
    }

    private static void ExecuteWindowMedia(ActionDefinition a)
    {
        var cmd = Enum.Parse<WindowMediaCommand>(a.Get("command"));
        var hwnd = NativeMethods.GetForegroundWindow();
        switch (cmd)
        {
            case WindowMediaCommand.MinimizeWindow: NativeMethods.ShowWindowAsync(hwnd, NativeMethods.SW_MINIMIZE); break;
            case WindowMediaCommand.MaximizeWindow: NativeMethods.ShowWindowAsync(hwnd, NativeMethods.SW_MAXIMIZE); break;
            case WindowMediaCommand.RestoreWindow: NativeMethods.ShowWindowAsync(hwnd, NativeMethods.SW_RESTORE); break;
            case WindowMediaCommand.CloseWindow:
                NativeMethods.PostMessageW(hwnd, NativeMethods.WM_SYSCOMMAND, NativeMethods.SC_CLOSE, 0); break;
            case WindowMediaCommand.SnapLeft: NativeInput.SendCombo(new[] { VkMap.ModifierVk("Win") }, VkMap.KeyVk("Left")); break;
            case WindowMediaCommand.SnapRight: NativeInput.SendCombo(new[] { VkMap.ModifierVk("Win") }, VkMap.KeyVk("Right")); break;
            case WindowMediaCommand.ShowDesktop: NativeInput.SendCombo(new[] { VkMap.ModifierVk("Win") }, VkMap.KeyVk("D")); break;
            case WindowMediaCommand.VolumeUp: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_VOLUME_UP); break;
            case WindowMediaCommand.VolumeDown: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_VOLUME_DOWN); break;
            case WindowMediaCommand.VolumeMute: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_VOLUME_MUTE); break;
            case WindowMediaCommand.MediaPlayPause: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_MEDIA_PLAY_PAUSE); break;
            case WindowMediaCommand.MediaNext: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_MEDIA_NEXT); break;
            case WindowMediaCommand.MediaPrevious: NativeInput.SendCombo(Array.Empty<ushort>(), VkMap.VK_MEDIA_PREV); break;
            case WindowMediaCommand.LockWorkstation: NativeMethods.LockWorkStation(); break;
        }
    }
}
```

- [ ] **Step 3: Build and commit**

```bash
dotnet build 2>&1 | tail -3
git add -A && git commit -m "feat(app): action executor for all four action types

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: `Build succeeded. 0 Error(s)`.

---

### Task 12: Gesture coordinator — end-to-end wiring (Windows milestone)

**Files:**
- Create: `src/SymbolCommander.App/Engine/GestureCoordinator.cs`
- Modify: `src/SymbolCommander.App/App.xaml.cs` (full replacement below)

**Interfaces:**
- Consumes: everything from Tasks 5–11.
- Produces (used by Task 13):
  - `sealed class GestureCoordinator : IDisposable` — ctor `(ConfigStore store, OverlayWindow overlay, Action<string,string,bool> notify)`;
    `void Start()`; `void ApplyConfig(AppConfig config, List<CustomSymbol> customs)` (thread-safe hot reload);
    `AppConfig CurrentConfig { get; }`; `SymbolCatalog Catalog { get; }`; `void SetGesturesEnabled(bool on)` (also persists)

- [ ] **Step 1: Write GestureCoordinator**

Write `src/SymbolCommander.App/Engine/GestureCoordinator.cs`:

```csharp
using System.Windows;
using SymbolCommander.App.Execution;
using SymbolCommander.App.Interop;
using SymbolCommander.App.Overlay;
using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Engine;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.App.Engine;

/// <summary>
/// Wires hooks → GestureEngine → recognizer → overlay/executor.
/// Hook callbacks run on the hook thread: they only feed the engine (fast, allocation-light).
/// Engine events marshal to the UI dispatcher for overlay drawing, recognition, and actions.
/// Config-derived state is swapped atomically under _gate for hot reload.
/// </summary>
public sealed class GestureCoordinator : IDisposable
{
    private readonly ConfigStore _store;
    private readonly OverlayWindow _overlay;
    private readonly Action<string, string, bool> _notify; // title, message, warning
    private readonly ActionExecutor _executor = new();
    private readonly GestureEngine _engine = new();
    private readonly MouseHook _mouseHook = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly HookHost _hookHost;

    private readonly object _gate = new();
    private AppConfig _config = null!;
    private SymbolCatalog _catalog = null!;
    private IReadOnlyList<SymbolTemplate> _activeTemplates = Array.Empty<SymbolTemplate>();
    private Dictionary<string, ActionDefinition> _bindingBySymbol = new();
    private HashSet<ushort>[] _hotkeyGroups = Array.Empty<HashSet<ushort>>();

    private readonly HashSet<ushort> _keysDown = new();
    private bool _hotkeyActive;

    private static readonly Dictionary<string, ushort[]> ModifierVariants = new()
    {
        ["Ctrl"] = new ushort[] { 0xA2, 0xA3 },  // L/R VK_CONTROL variants
        ["Alt"] = new ushort[] { 0xA4, 0xA5 },
        ["Shift"] = new ushort[] { 0xA0, 0xA1 },
        ["Win"] = new ushort[] { 0x5B, 0x5C },
    };
    private const ushort VK_ESCAPE = 0x1B;

    public AppConfig CurrentConfig { get { lock (_gate) return _config; } }
    public SymbolCatalog Catalog { get { lock (_gate) return _catalog; } }

    public GestureCoordinator(ConfigStore store, OverlayWindow overlay, Action<string, string, bool> notify)
    {
        _store = store;
        _overlay = overlay;
        _notify = notify;
        _hookHost = new HookHost(_mouseHook, _keyboardHook, () => _engine.State == EngineState.Idle);

        _executor.ActionFailed += msg => OnUi(() => _notify("Action failed", msg, true));
        _store.LoadWarning += msg => OnUi(() => _notify("Symbol Commander", msg, true));

        _engine.TrailStarted += p => OnUi(() => _overlay.StartTrail(p.X, p.Y));
        _engine.TrailPointAdded += p => OnUi(() => _overlay.AddTrailPoint(p.X, p.Y));
        _engine.Cancelled += () => OnUi(_overlay.CancelTrail);
        _engine.StrokeCompleted += stroke => OnUi(() => OnStrokeCompleted(stroke));
        _engine.ClickPassthroughRequested += (p, source) =>
        {
            if (source == TriggerSource.RightButton) NativeInput.SendRightClick();
        };

        _mouseHook.MouseEvent += OnMouseEvent;
        _keyboardHook.KeyEvent += OnKeyEvent;
    }

    public void Start()
    {
        ApplyConfig(_store.Load(), _store.LoadCustomSymbols());
        _hookHost.Start();
    }

    public void ApplyConfig(AppConfig config, List<CustomSymbol> customs)
    {
        var catalog = new SymbolCatalog(customs);
        var actionById = config.Actions.ToDictionary(a => a.Id);
        var bindingBySymbol = new Dictionary<string, ActionDefinition>();
        foreach (var b in config.Bindings.Where(b => b.Enabled))
            if (actionById.TryGetValue(b.ActionId, out var action))
                bindingBySymbol[b.SymbolId] = action;

        string[] hotkeyMods;
        try
        {
            hotkeyMods = config.Settings.HotkeyModifiers
                .Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (hotkeyMods.Any(m => !ModifierVariants.ContainsKey(m)))
                throw new FormatException();
        }
        catch (FormatException)
        {
            hotkeyMods = new[] { "Ctrl", "Alt" };
        }

        lock (_gate)
        {
            _config = config;
            _catalog = catalog;
            _bindingBySymbol = bindingBySymbol;
            // recognize only against symbols that have an enabled binding — an unbound
            // symbol should never steal a match from a bound one
            _activeTemplates = catalog.TemplatesFor(bindingBySymbol.Keys);
            _hotkeyGroups = hotkeyMods.Select(m => ModifierVariants[m].ToHashSet()).ToArray();
        }
        OnUi(() => _overlay.ConfigureTrail(config.Settings.TrailColor, config.Settings.TrailThickness));
    }

    public void SetGesturesEnabled(bool on)
    {
        AppConfig config;
        lock (_gate) { _config.Settings.GesturesEnabled = on; config = _config; }
        _store.Save(config);
    }

    // ---- hook thread ----

    private void OnMouseEvent(object? sender, MouseHookEventArgs e)
    {
        if (e.Injected) return;
        bool gesturesOn, rightTriggerOn;
        lock (_gate)
        {
            gesturesOn = _config.Settings.GesturesEnabled;
            rightTriggerOn = _config.Settings.RightButtonTriggerEnabled;
        }
        if (!gesturesOn) return;

        switch (e.Message)
        {
            case MouseHook.WM_RBUTTONDOWN when rightTriggerOn:
                _engine.TriggerDown(new GesturePoint(e.X, e.Y), TriggerSource.RightButton);
                e.Suppress = true;
                break;
            case MouseHook.WM_RBUTTONUP when _engine.ActiveSource == TriggerSource.RightButton:
                _engine.TriggerUp(new GesturePoint(e.X, e.Y));
                e.Suppress = true;
                break;
            case MouseHook.WM_RBUTTONUP when rightTriggerOn && _engine.State == EngineState.Idle:
                // release after an Escape-cancelled right-button gesture
                e.Suppress = true;
                break;
            case MouseHook.WM_MOUSEMOVE:
                _engine.PointerMoved(new GesturePoint(e.X, e.Y));
                break;
        }
    }

    private void OnKeyEvent(object? sender, KeyboardHookEventArgs e)
    {
        if (e.Injected) return;

        if (e.IsDown) _keysDown.Add((ushort)e.VkCode);
        else _keysDown.Remove((ushort)e.VkCode);

        if (e.IsDown && e.VkCode == VK_ESCAPE && _engine.State == EngineState.Drawing)
        {
            _engine.Cancel();
            e.Suppress = true;
            return;
        }

        bool gesturesOn, hotkeyOn;
        HashSet<ushort>[] groups;
        lock (_gate)
        {
            gesturesOn = _config.Settings.GesturesEnabled;
            hotkeyOn = _config.Settings.HotkeyTriggerEnabled;
            groups = _hotkeyGroups;
        }
        if (!gesturesOn || !hotkeyOn || groups.Length == 0) return;

        bool comboHeld = groups.All(g => g.Overlaps(_keysDown));
        if (comboHeld && !_hotkeyActive)
        {
            _hotkeyActive = true;
            var (x, y) = NativeInput.CursorPos();
            _engine.TriggerDown(new GesturePoint(x, y), TriggerSource.Hotkey);
        }
        else if (!comboHeld && _hotkeyActive)
        {
            _hotkeyActive = false;
            if (_engine.ActiveSource == TriggerSource.Hotkey)
            {
                var (x, y) = NativeInput.CursorPos();
                _engine.TriggerUp(new GesturePoint(x, y));
            }
        }
    }

    // ---- UI thread ----

    private void OnStrokeCompleted(IReadOnlyList<GesturePoint> stroke)
    {
        IReadOnlyList<SymbolTemplate> templates;
        Dictionary<string, ActionDefinition> bindings;
        double threshold;
        SymbolCatalog catalog;
        lock (_gate)
        {
            templates = _activeTemplates;
            bindings = _bindingBySymbol;
            threshold = _config.Settings.Sensitivity;
            catalog = _catalog;
        }

        var result = ProtractorRecognizer.Recognize(stroke, templates, threshold);
        if (result.IsMatch && bindings.TryGetValue(result.SymbolId!, out var action))
        {
            _overlay.EndTrailRecognized($"{catalog.NameOf(result.SymbolId!)}  →  {action.Name}");
            _executor.Execute(action);
        }
        else
        {
            _overlay.EndTrailRejected();
        }
    }

    private static void OnUi(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) return;
        if (dispatcher.CheckAccess()) action();
        else dispatcher.BeginInvoke(action);
    }

    public void Dispose()
    {
        _hookHost.Dispose();
        _mouseHook.Dispose();
        _keyboardHook.Dispose();
    }
}
```

- [ ] **Step 2: Rewire App.xaml.cs**

Replace the full contents of `src/SymbolCommander.App/App.xaml.cs` with:

```csharp
using System.IO;
using System.Threading;
using System.Windows;
using SymbolCommander.App.Engine;
using SymbolCommander.App.Overlay;
using SymbolCommander.App.Tray;
using SymbolCommander.Core.Config;

namespace SymbolCommander.App;

public partial class App : System.Windows.Application
{
    private const string MutexName = @"Local\SymbolCommander.SingleInstance";
    private const string ShowSettingsEventName = @"Local\SymbolCommander.ShowSettings";

    private Mutex? _instanceMutex;
    private EventWaitHandle? _showSettingsEvent;

    public static new App Current => (App)System.Windows.Application.Current;
    public ConfigStore ConfigStore { get; private set; } = null!;
    public TrayIcon Tray { get; private set; } = null!;
    public GestureCoordinator Coordinator { get; private set; } = null!;
    public OverlayWindow Overlay { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirst);
        _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        if (!isFirst)
        {
            _showSettingsEvent.Set();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SymbolCommander");
        ConfigStore = new ConfigStore(configDir);

        Tray = new TrayIcon();
        Overlay = new OverlayWindow();
        Overlay.Show();

        Coordinator = new GestureCoordinator(ConfigStore, Overlay, Tray.ShowNotification);
        Coordinator.Start();
        Tray.SetGesturesEnabled(Coordinator.CurrentConfig.Settings.GesturesEnabled);

        Tray.ExitRequested += Shutdown;
        Tray.SettingsRequested += OpenSettings;
        Tray.GesturesToggled += on => Coordinator.SetGesturesEnabled(on);

        var waiter = new Thread(() =>
        {
            while (_showSettingsEvent.WaitOne())
                Dispatcher.BeginInvoke(OpenSettings);
        }) { IsBackground = true };
        waiter.Start();
    }

    private void OpenSettings()
    {
        // Task 13 replaces this with the real settings window
        Tray.ShowNotification("Symbol Commander", "Settings UI arrives in a later task.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Coordinator?.Dispose();
        Tray?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 3: Build, test, publish**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet build 2>&1 | tail -3 && dotnet test 2>&1 | tail -3
dotnet publish src/SymbolCommander.App -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true 2>&1 | tail -3
```

Expected: build succeeded, all tests pass, publish emits `SymbolCommander.exe`.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat(app): gesture coordinator - first end-to-end wiring

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

- [ ] **Step 5: WINDOWS MILESTONE CHECKLIST #1 (user, on the Windows PC)**

Copy `src/SymbolCommander.App/bin/Release/net8.0-windows/win-x64/publish/SymbolCommander.exe` to the Windows PC and run it. Ask the user to verify and report back:

1. Tray icon appears; no window opens.
2. **Plain right-click still works** — right-click the desktop: context menu opens normally.
3. Hold right button and draw a large `M` → blue trail follows the cursor → on release, toast shows "M → Minimize window" and the active window minimizes.
4. Draw `W` → default browser opens.
5. Draw a straight line up → volume goes up (volume OSD appears); down → volume down.
6. Draw a circle (either direction) → desktop shows (Win+D behavior).
7. Draw junk (a tight scribble) → trail turns red and fades; nothing fires.
8. Press Esc mid-draw → trail vanishes; releasing the button does nothing (no context menu).
9. Hold Ctrl+Alt and draw `M` with the mouse or trackpad → same as (3). On a laptop: draw with one-finger trackpad glide while holding Ctrl+Alt.
10. Tray → untick "Gestures enabled" → right-click-drag behaves like plain Windows again; re-tick → gestures return.
11. Tray → Exit → icon disappears, process ends (check Task Manager).

**Do not proceed to Task 13 until the user confirms this checklist.** Fix reported failures first (likely areas: DPI trail offset on scaled displays — check `ToCanvas`; passthrough timing; hotkey edge transitions).

---

### Task 13: Settings window shell + General tab + startup registry

**Files:**
- Create: `src/SymbolCommander.App/StartupManager.cs`
- Create: `src/SymbolCommander.App/Settings/SettingsWindow.xaml` + `.xaml.cs`
- Create: `src/SymbolCommander.App/Settings/GeneralTab.xaml` + `.xaml.cs`
- Modify: `src/SymbolCommander.App/App.xaml.cs` — replace `OpenSettings()` only (shown below)

**Interfaces:**
- Consumes: `GestureCoordinator`, `ConfigStore`, `AppConfig` (Tasks 7, 12).
- Produces (used by Tasks 14–16):
  - `static class StartupManager` — `void Apply(bool enabled)`
  - `partial class SettingsWindow : Window` — ctor `(GestureCoordinator, ConfigStore)`; `AppConfig Working { get; }` (deep clone being edited); `ConfigStore Store { get; }`; `GestureCoordinator Coordinator { get; }`; `void ApplyWorking()` (collect tabs → save → hot-reload → registry)
  - Tab convention (Tasks 14–16 follow it): each tab exposes `void Load(SettingsWindow owner)` (called once from SettingsWindow ctor), `void Reload()` (called when the tab is selected), `void CollectInto(AppConfig working)` (called before apply).

- [ ] **Step 1: Write StartupManager**

Write `src/SymbolCommander.App/StartupManager.cs`:

```csharp
using Microsoft.Win32;

namespace SymbolCommander.App;

public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SymbolCommander";

    public static void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key is null) return;
        if (enabled && Environment.ProcessPath is { } exe) key.SetValue(ValueName, $"\"{exe}\"");
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
```

- [ ] **Step 2: Write GeneralTab (Task 13 version — Task 14 extends this same file)**

Write `src/SymbolCommander.App/Settings/GeneralTab.xaml`:

```xml
<UserControl x:Class="SymbolCommander.App.Settings.GeneralTab"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="12" x:Name="Root">
            <GroupBox Header="Triggers" Padding="8">
                <StackPanel>
                    <CheckBox x:Name="RightButtonCheck" Content="Hold right mouse button and draw" Margin="0,2"/>
                    <StackPanel Orientation="Horizontal" Margin="0,2">
                        <CheckBox x:Name="HotkeyCheck" Content="Hold hotkey and draw (trackpad-friendly):"
                                  VerticalAlignment="Center"/>
                        <ComboBox x:Name="HotkeyCombo" Width="140" Margin="8,0,0,0"/>
                    </StackPanel>
                </StackPanel>
            </GroupBox>
            <GroupBox Header="Recognition" Padding="8" Margin="0,8,0,0">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="Sensitivity:" VerticalAlignment="Center"/>
                    <Slider x:Name="SensitivitySlider" Width="220" Minimum="0.5" Maximum="0.95"
                            TickFrequency="0.05" IsSnapToTickEnabled="True" Margin="8,0"/>
                    <TextBlock x:Name="SensitivityLabel" VerticalAlignment="Center" Width="40"/>
                    <TextBlock Text="(higher = stricter matching)" VerticalAlignment="Center" Foreground="Gray"/>
                </StackPanel>
            </GroupBox>
            <GroupBox Header="Trail" Padding="8" Margin="0,8,0,0">
                <StackPanel Orientation="Horizontal">
                    <TextBlock Text="Color:" VerticalAlignment="Center"/>
                    <ComboBox x:Name="TrailColorCombo" Width="110" Margin="8,0"
                              SelectedValuePath="Tag" DisplayMemberPath="Content"/>
                    <TextBlock Text="Thickness:" VerticalAlignment="Center" Margin="16,0,0,0"/>
                    <Slider x:Name="ThicknessSlider" Width="140" Minimum="2" Maximum="8"
                            TickFrequency="1" IsSnapToTickEnabled="True" Margin="8,0"/>
                </StackPanel>
            </GroupBox>
            <GroupBox Header="System" Padding="8" Margin="0,8,0,0">
                <StackPanel>
                    <CheckBox x:Name="StartupCheck" Content="Start with Windows" Margin="0,2"/>
                    <CheckBox x:Name="EnabledCheck" Content="Gestures enabled" Margin="0,2"/>
                </StackPanel>
            </GroupBox>
            <!-- Task 14 inserts the Actions GroupBox here -->
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

Write `src/SymbolCommander.App/Settings/GeneralTab.xaml.cs`:

```csharp
using System.Windows.Controls;
using SymbolCommander.Core.Config;

namespace SymbolCommander.App.Settings;

public partial class GeneralTab : UserControl
{
    private SettingsWindow _owner = null!;
    private static readonly string[] HotkeyPresets =
        { "Ctrl+Alt", "Ctrl+Shift", "Alt+Shift", "Ctrl+Alt+Shift", "Win+Alt" };

    public GeneralTab()
    {
        InitializeComponent();
        foreach (var p in HotkeyPresets) HotkeyCombo.Items.Add(p);
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Blue", Tag = "#3399FF" });
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Red", Tag = "#E74C3C" });
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Green", Tag = "#2ECC71" });
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Orange", Tag = "#E67E22" });
        TrailColorCombo.Items.Add(new ComboBoxItem { Content = "Purple", Tag = "#9B59B6" });
        SensitivitySlider.ValueChanged += (_, _) => SensitivityLabel.Text = SensitivitySlider.Value.ToString("0.00");
    }

    public void Load(SettingsWindow owner)
    {
        _owner = owner;
        var s = owner.Working.Settings;
        RightButtonCheck.IsChecked = s.RightButtonTriggerEnabled;
        HotkeyCheck.IsChecked = s.HotkeyTriggerEnabled;
        HotkeyCombo.SelectedItem = HotkeyPresets.Contains(s.HotkeyModifiers) ? s.HotkeyModifiers : HotkeyPresets[0];
        SensitivitySlider.Value = s.Sensitivity;
        SensitivityLabel.Text = s.Sensitivity.ToString("0.00");
        TrailColorCombo.SelectedValue = s.TrailColor;
        if (TrailColorCombo.SelectedIndex < 0) TrailColorCombo.SelectedIndex = 0;
        ThicknessSlider.Value = s.TrailThickness;
        StartupCheck.IsChecked = s.StartWithWindows;
        EnabledCheck.IsChecked = s.GesturesEnabled;
    }

    public void Reload() { }

    public void CollectInto(AppConfig working)
    {
        var s = working.Settings;
        s.RightButtonTriggerEnabled = RightButtonCheck.IsChecked == true;
        s.HotkeyTriggerEnabled = HotkeyCheck.IsChecked == true;
        s.HotkeyModifiers = HotkeyCombo.SelectedItem as string ?? "Ctrl+Alt";
        s.Sensitivity = SensitivitySlider.Value;
        s.TrailColor = TrailColorCombo.SelectedValue as string ?? "#3399FF";
        s.TrailThickness = ThicknessSlider.Value;
        s.StartWithWindows = StartupCheck.IsChecked == true;
        s.GesturesEnabled = EnabledCheck.IsChecked == true;
    }
}
```

- [ ] **Step 3: Write SettingsWindow**

Write `src/SymbolCommander.App/Settings/SettingsWindow.xaml`:

```xml
<Window x:Class="SymbolCommander.App.Settings.SettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:SymbolCommander.App.Settings"
        Title="Symbol Commander Settings" Width="680" Height="600"
        WindowStartupLocation="CenterScreen">
    <DockPanel>
        <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" HorizontalAlignment="Right" Margin="8">
            <Button Content="OK" Width="80" Margin="4" Click="Ok_Click"/>
            <Button Content="Cancel" Width="80" Margin="4" Click="Cancel_Click"/>
            <Button Content="Apply" Width="80" Margin="4" Click="Apply_Click"/>
        </StackPanel>
        <TabControl x:Name="Tabs" SelectionChanged="Tabs_SelectionChanged">
            <!-- Tasks 15/16 insert Bindings and Symbols TabItems BEFORE this one -->
            <TabItem Header="Actions &amp; General">
                <local:GeneralTab x:Name="General"/>
            </TabItem>
        </TabControl>
    </DockPanel>
</Window>
```

Write `src/SymbolCommander.App/Settings/SettingsWindow.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using SymbolCommander.App.Engine;
using SymbolCommander.Core.Config;

namespace SymbolCommander.App.Settings;

public partial class SettingsWindow : Window
{
    public AppConfig Working { get; }
    public ConfigStore Store { get; }
    public GestureCoordinator Coordinator { get; }

    public SettingsWindow(GestureCoordinator coordinator, ConfigStore store)
    {
        InitializeComponent();
        Coordinator = coordinator;
        Store = store;
        Working = coordinator.CurrentConfig.Clone();
        General.Load(this);
        // Tasks 15/16 add: Bindings.Load(this); Symbols.Load(this);
    }

    public void ApplyWorking()
    {
        General.CollectInto(Working);
        // Tasks 15/16 add: Bindings.CollectInto(Working); (Symbols tab saves customs directly)
        var snapshot = Working.Clone();
        Store.Save(snapshot);
        Coordinator.ApplyConfig(snapshot, Store.LoadCustomSymbols());
        StartupManager.Apply(snapshot.Settings.StartWithWindows);
        App.Current.Tray.SetGesturesEnabled(snapshot.Settings.GesturesEnabled);
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.Source, Tabs)) return;
        // Tasks 15/16 add per-tab Reload() calls here
        General.Reload();
    }

    private void Ok_Click(object sender, RoutedEventArgs e) { ApplyWorking(); Close(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    private void Apply_Click(object sender, RoutedEventArgs e) => ApplyWorking();
}
```

- [ ] **Step 4: Wire it into App**

In `src/SymbolCommander.App/App.xaml.cs`: add field + replace the placeholder `OpenSettings()` method with:

```csharp
    private Settings.SettingsWindow? _settingsWindow;

    private void OpenSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new Settings.SettingsWindow(Coordinator, ConfigStore);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }
```

- [ ] **Step 5: Build and commit**

```bash
dotnet build 2>&1 | tail -3
git add -A && git commit -m "feat(app): settings window with general tab, start-with-Windows

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: `Build succeeded. 0 Error(s)`.

---

### Task 14: Action management (editor dialog + keystroke recorder)

**Files:**
- Create: `src/SymbolCommander.App/Settings/KeystrokeRecorderBox.cs`
- Create: `src/SymbolCommander.App/Settings/ActionEditorDialog.xaml` + `.xaml.cs`
- Modify: `src/SymbolCommander.App/Settings/GeneralTab.xaml` — add the Actions GroupBox where the Task 13 comment marks the spot
- Modify: `src/SymbolCommander.App/Settings/GeneralTab.xaml.cs` — add action-list logic

**Interfaces:**
- Consumes: `ActionDefinition`, `ActionType`, `WindowMediaCommand`, `ActionValidator` (Task 6); SettingsWindow tab convention (Task 13).
- Produces (used by Task 15):
  - `partial class ActionEditorDialog : Window` — ctor `(ActionDefinition? existing)`; `ActionDefinition? Result` (non-null after OK); validates via `ActionValidator` before accepting
  - `sealed class KeystrokeRecorderBox : TextBox` — press a combo, it types itself (e.g. "Ctrl+Shift+T"); manual editing allowed for sequences

- [ ] **Step 1: Write KeystrokeRecorderBox**

Write `src/SymbolCommander.App/Settings/KeystrokeRecorderBox.cs`:

```csharp
using System.Windows.Controls;
using System.Windows.Input;

namespace SymbolCommander.App.Settings;

/// <summary>TextBox that records a pressed key combo as text ("Ctrl+Shift+T").
/// Text stays editable by hand so users can type sequences ("Ctrl+K, Ctrl+S").</summary>
public sealed class KeystrokeRecorderBox : TextBox
{
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            e.Handled = true;
            return;
        }
        if (key == Key.Tab) return; // keep keyboard navigation working

        var name = MapKey(key);
        if (name is null) { base.OnPreviewKeyDown(e); return; }

        var parts = new List<string>();
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(name);
        Text = string.Join("+", parts);
        CaretIndex = Text.Length;
        e.Handled = true;
    }

    private static string? MapKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.D0 and <= Key.D9 => key.ToString()[1..],
        >= Key.F1 and <= Key.F12 => key.ToString(),
        Key.Return => "Enter", Key.Space => "Space", Key.Escape => "Esc",
        Key.Back => "Backspace", Key.Delete => "Delete", Key.Insert => "Insert",
        Key.Home => "Home", Key.End => "End", Key.PageUp => "PageUp", Key.Next => "PageDown",
        Key.Up => "Up", Key.Down => "Down", Key.Left => "Left", Key.Right => "Right",
        _ => null,
    };
}
```

- [ ] **Step 2: Write ActionEditorDialog**

Write `src/SymbolCommander.App/Settings/ActionEditorDialog.xaml`:

```xml
<Window x:Class="SymbolCommander.App.Settings.ActionEditorDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:SymbolCommander.App.Settings"
        Title="Edit Action" Width="440" SizeToContent="Height"
        WindowStartupLocation="CenterOwner" ResizeMode="NoResize">
    <StackPanel Margin="12">
        <TextBlock Text="Name:"/>
        <TextBox x:Name="NameBox" Margin="0,2,0,8"/>
        <TextBlock Text="Type:"/>
        <ComboBox x:Name="TypeCombo" Margin="0,2,0,8" SelectionChanged="TypeCombo_SelectionChanged"/>

        <StackPanel x:Name="KeystrokePanel">
            <TextBlock Text="Keystroke (click below and press the combo; edit by hand for sequences):"/>
            <local:KeystrokeRecorderBox x:Name="KeysBox" Margin="0,2,0,8"/>
        </StackPanel>
        <StackPanel x:Name="LaunchPanel">
            <TextBlock Text="Program, file, folder, or URL:"/>
            <DockPanel Margin="0,2,0,8">
                <Button DockPanel.Dock="Right" Content="Browse…" Width="80" Margin="6,0,0,0" Click="Browse_Click"/>
                <TextBox x:Name="TargetBox"/>
            </DockPanel>
        </StackPanel>
        <StackPanel x:Name="WindowMediaPanel">
            <TextBlock Text="Command:"/>
            <ComboBox x:Name="CommandCombo" Margin="0,2,0,8"/>
        </StackPanel>
        <StackPanel x:Name="ShellPanel">
            <TextBlock Text="Command line (runs via cmd /c):"/>
            <TextBox x:Name="CommandLineBox" Margin="0,2,0,4"/>
            <CheckBox x:Name="HiddenCheck" Content="Run hidden (no console window)" IsChecked="True" Margin="0,0,0,8"/>
        </StackPanel>

        <TextBlock x:Name="ErrorLabel" Foreground="Firebrick" TextWrapping="Wrap" Visibility="Collapsed"/>
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,8,0,0">
            <Button Content="OK" Width="80" Margin="4,0" Click="Ok_Click"/>
            <Button Content="Cancel" Width="80" IsCancel="True"/>
        </StackPanel>
    </StackPanel>
</Window>
```

Write `src/SymbolCommander.App/Settings/ActionEditorDialog.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using SymbolCommander.Core.Actions;

namespace SymbolCommander.App.Settings;

public partial class ActionEditorDialog : Window
{
    public ActionDefinition? Result { get; private set; }
    private readonly string _id;

    public ActionEditorDialog(ActionDefinition? existing)
    {
        InitializeComponent();
        foreach (var t in Enum.GetValues<ActionType>()) TypeCombo.Items.Add(t);
        foreach (var c in Enum.GetValues<WindowMediaCommand>()) CommandCombo.Items.Add(c);
        CommandCombo.SelectedIndex = 0;

        _id = existing?.Id ?? Guid.NewGuid().ToString("N");
        NameBox.Text = existing?.Name ?? "";
        TypeCombo.SelectedItem = existing?.Type ?? ActionType.Keystroke;
        if (existing is not null)
        {
            KeysBox.Text = existing.Get("keys");
            TargetBox.Text = existing.Get("target");
            if (Enum.TryParse<WindowMediaCommand>(existing.Get("command"), out var cmd))
                CommandCombo.SelectedItem = cmd;
            CommandLineBox.Text = existing.Get("commandLine");
            HiddenCheck.IsChecked = !string.Equals(existing.Get("hidden"), "false", StringComparison.OrdinalIgnoreCase);
        }
        UpdatePanels();
    }

    private void TypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePanels();

    private void UpdatePanels()
    {
        var t = (ActionType?)TypeCombo.SelectedItem ?? ActionType.Keystroke;
        KeystrokePanel.Visibility = t == ActionType.Keystroke ? Visibility.Visible : Visibility.Collapsed;
        LaunchPanel.Visibility = t == ActionType.Launch ? Visibility.Visible : Visibility.Collapsed;
        WindowMediaPanel.Visibility = t == ActionType.WindowMedia ? Visibility.Visible : Visibility.Collapsed;
        ShellPanel.Visibility = t == ActionType.Shell ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Choose a program or file" };
        if (dlg.ShowDialog(this) == true) TargetBox.Text = dlg.FileName;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var t = (ActionType?)TypeCombo.SelectedItem ?? ActionType.Keystroke;
        var action = new ActionDefinition { Id = _id, Name = NameBox.Text.Trim(), Type = t };
        switch (t)
        {
            case ActionType.Keystroke: action.Parameters["keys"] = KeysBox.Text.Trim(); break;
            case ActionType.Launch: action.Parameters["target"] = TargetBox.Text.Trim(); break;
            case ActionType.WindowMedia: action.Parameters["command"] = CommandCombo.SelectedItem?.ToString() ?? ""; break;
            case ActionType.Shell:
                action.Parameters["commandLine"] = CommandLineBox.Text.Trim();
                action.Parameters["hidden"] = HiddenCheck.IsChecked == true ? "true" : "false";
                break;
        }
        var error = ActionValidator.Validate(action);
        if (error is not null)
        {
            ErrorLabel.Text = error;
            ErrorLabel.Visibility = Visibility.Visible;
            return;
        }
        Result = action;
        DialogResult = true;
    }
}
```

- [ ] **Step 3: Add the Actions group to GeneralTab**

In `src/SymbolCommander.App/Settings/GeneralTab.xaml`, replace the comment line `<!-- Task 14 inserts the Actions GroupBox here -->` with:

```xml
            <GroupBox Header="Actions" Padding="8" Margin="0,8,0,0">
                <DockPanel>
                    <StackPanel DockPanel.Dock="Right" Margin="8,0,0,0">
                        <Button Content="Add…" Width="80" Margin="0,2" Click="AddAction_Click"/>
                        <Button Content="Edit…" Width="80" Margin="0,2" Click="EditAction_Click"/>
                        <Button Content="Remove" Width="80" Margin="0,2" Click="RemoveAction_Click"/>
                    </StackPanel>
                    <ListBox x:Name="ActionsList" Height="160" DisplayMemberPath="Display"/>
                </DockPanel>
            </GroupBox>
```

In `src/SymbolCommander.App/Settings/GeneralTab.xaml.cs`, add inside the class (and `using System.Windows;` + `using SymbolCommander.Core.Actions;` to the usings):

```csharp
    private sealed record ActionRow(ActionDefinition Action)
    {
        public string Display => $"{Action.Name}  ({Action.Type})";
    }

    private void RefreshActions()
    {
        ActionsList.Items.Clear();
        foreach (var a in _owner.Working.Actions) ActionsList.Items.Add(new ActionRow(a));
    }

    private void AddAction_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ActionEditorDialog(null) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result is { } action)
        {
            _owner.Working.Actions.Add(action);
            RefreshActions();
        }
    }

    private void EditAction_Click(object sender, RoutedEventArgs e)
    {
        if (ActionsList.SelectedItem is not ActionRow row) return;
        var dlg = new ActionEditorDialog(row.Action) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true && dlg.Result is { } edited)
        {
            var i = _owner.Working.Actions.FindIndex(a => a.Id == edited.Id);
            if (i >= 0) _owner.Working.Actions[i] = edited;
            RefreshActions();
        }
    }

    private void RemoveAction_Click(object sender, RoutedEventArgs e)
    {
        if (ActionsList.SelectedItem is not ActionRow row) return;
        int used = _owner.Working.Bindings.RemoveAll(b => b.ActionId == row.Action.Id);
        if (used > 0)
            MessageBox.Show($"Also removed {used} binding(s) that used \"{row.Action.Name}\".",
                "Symbol Commander", MessageBoxButton.OK, MessageBoxImage.Information);
        _owner.Working.Actions.Remove(row.Action);
        RefreshActions();
    }
```

Then append `RefreshActions();` at the end of the existing `Load(SettingsWindow owner)` method body, and change `Reload()` to:

```csharp
    public void Reload() => RefreshActions();
```

- [ ] **Step 4: Build and commit**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet build 2>&1 | tail -3
git add -A && git commit -m "feat(app): action management UI with keystroke recorder

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: `Build succeeded. 0 Error(s)`.

---

### Task 15: Bindings tab + drawing canvas

**Files:**
- Create: `src/SymbolCommander.App/Settings/DrawingCanvas.cs`
- Create: `src/SymbolCommander.App/Settings/BindingsTab.xaml` + `.xaml.cs`
- Modify: `src/SymbolCommander.App/Settings/SettingsWindow.xaml` + `.xaml.cs` — register the tab

**Interfaces:**
- Consumes: tab convention (Task 13), `SymbolCatalog`, `ProtractorRecognizer`, `Binding` (Tasks 3, 6, 7).
- Produces (used by Task 16):
  - `sealed class DrawingCanvas : Border` — draw with left button held; `event Action<IReadOnlyList<GesturePoint>>? StrokeDrawn`; `void ClearInk()`

- [ ] **Step 1: Write DrawingCanvas**

Write `src/SymbolCommander.App/Settings/DrawingCanvas.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.App.Settings;

/// <summary>In-window stroke capture for the test area and symbol training.
/// Plain WPF mouse events — global hooks are not involved here.</summary>
public sealed class DrawingCanvas : Border
{
    public event Action<IReadOnlyList<GesturePoint>>? StrokeDrawn;

    private readonly Canvas _canvas = new();
    private Polyline? _line;
    private List<GesturePoint> _points = new();

    public DrawingCanvas()
    {
        Background = Brushes.White;
        BorderBrush = Brushes.Gray;
        BorderThickness = new Thickness(1);
        ClipToBounds = true;
        Child = _canvas;
        Cursor = Cursors.Pen;
    }

    public void ClearInk()
    {
        _canvas.Children.Clear();
        _line = null;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        ClearInk();
        CaptureMouse();
        var p = e.GetPosition(_canvas);
        _points = new List<GesturePoint> { new(p.X, p.Y) };
        _line = new Polyline
        {
            Stroke = Brushes.DodgerBlue, StrokeThickness = 3,
            StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Points = new PointCollection { p },
        };
        _canvas.Children.Add(_line);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!IsMouseCaptured || _line is null) return;
        var p = e.GetPosition(_canvas);
        _points.Add(new GesturePoint(p.X, p.Y));
        _line.Points.Add(p);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (!IsMouseCaptured) return;
        ReleaseMouseCapture();
        if (_points.Count >= 2) StrokeDrawn?.Invoke(_points.ToArray());
    }
}
```

- [ ] **Step 2: Write BindingsTab**

Write `src/SymbolCommander.App/Settings/BindingsTab.xaml`:

```xml
<UserControl x:Class="SymbolCommander.App.Settings.BindingsTab"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:SymbolCommander.App.Settings">
    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="170"/>
        </Grid.RowDefinitions>

        <DataGrid x:Name="BindingsGrid" Grid.Row="0" AutoGenerateColumns="False" CanUserAddRows="False"
                  HeadersVisibility="Column" SelectionMode="Single">
            <DataGrid.Columns>
                <DataGridTemplateColumn Header="Symbol" Width="*">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <ComboBox ItemsSource="{Binding Symbols}" DisplayMemberPath="Name"
                                      SelectedValuePath="Id" SelectedValue="{Binding SymbolId, UpdateSourceTrigger=PropertyChanged}"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridTemplateColumn Header="Action" Width="*">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <ComboBox ItemsSource="{Binding Actions}" DisplayMemberPath="Name"
                                      SelectedValuePath="Id" SelectedValue="{Binding ActionId, UpdateSourceTrigger=PropertyChanged}"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
                <DataGridCheckBoxColumn Header="Enabled" Width="70"
                    Binding="{Binding Enabled, UpdateSourceTrigger=PropertyChanged}"/>
            </DataGrid.Columns>
        </DataGrid>

        <StackPanel Grid.Row="1" Orientation="Horizontal" Margin="0,6">
            <Button Content="Add binding" Width="100" Click="Add_Click"/>
            <Button Content="Remove" Width="80" Margin="6,0" Click="Remove_Click"/>
        </StackPanel>

        <GroupBox Grid.Row="2" Header="Test area — draw here to see what recognizes (nothing fires)">
            <DockPanel>
                <TextBlock x:Name="TestResult" DockPanel.Dock="Bottom" Margin="4" FontWeight="SemiBold"/>
                <local:DrawingCanvas x:Name="TestCanvas"/>
            </DockPanel>
        </GroupBox>
    </Grid>
</UserControl>
```

Write `src/SymbolCommander.App/Settings/BindingsTab.xaml.cs`:

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SymbolCommander.Core.Actions;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.App.Settings;

public partial class BindingsTab : UserControl
{
    public sealed record Choice(string Id, string Name);

    public sealed class BindingRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public IReadOnlyList<Choice> Symbols { get; init; } = Array.Empty<Choice>();
        public IReadOnlyList<Choice> Actions { get; init; } = Array.Empty<Choice>();

        private string _symbolId = "", _actionId = "";
        private bool _enabled = true;

        public string SymbolId { get => _symbolId; set { _symbolId = value; Notify(nameof(SymbolId)); } }
        public string ActionId { get => _actionId; set { _actionId = value; Notify(nameof(ActionId)); } }
        public bool Enabled { get => _enabled; set { _enabled = value; Notify(nameof(Enabled)); } }
        private void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    private SettingsWindow _owner = null!;
    private readonly ObservableCollection<BindingRow> _rows = new();
    private SymbolCatalog _catalog = new(Array.Empty<CustomSymbol>());
    private bool _loaded;

    public BindingsTab()
    {
        InitializeComponent();
        BindingsGrid.ItemsSource = _rows;
        TestCanvas.StrokeDrawn += OnTestStroke;
    }

    public void Load(SettingsWindow owner)
    {
        _owner = owner;
        Reload();
        _loaded = true;
    }

    public void Reload()
    {
        // keep unsaved row edits when the user tabs away and back
        if (_loaded) CollectInto(_owner.Working);
        _catalog = new SymbolCatalog(_owner.Store.LoadCustomSymbols());
        var symbols = _catalog.All.Select(s => new Choice(s.Id, s.IsBuiltIn ? s.Name : $"{s.Name} (custom)")).ToList();
        var actions = _owner.Working.Actions.Select(a => new Choice(a.Id, a.Name)).ToList();
        _rows.Clear();
        foreach (var b in _owner.Working.Bindings)
            _rows.Add(new BindingRow { Symbols = symbols, Actions = actions,
                SymbolId = b.SymbolId, ActionId = b.ActionId, Enabled = b.Enabled });
    }

    public void CollectInto(AppConfig working)
    {
        working.Bindings.Clear();
        foreach (var r in _rows.Where(r => r.SymbolId != "" && r.ActionId != ""))
            working.Bindings.Add(new Binding { SymbolId = r.SymbolId, ActionId = r.ActionId, Enabled = r.Enabled });
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var symbols = _catalog.All.Select(s => new Choice(s.Id, s.IsBuiltIn ? s.Name : $"{s.Name} (custom)")).ToList();
        var actions = _owner.Working.Actions.Select(a => new Choice(a.Id, a.Name)).ToList();
        _rows.Add(new BindingRow { Symbols = symbols, Actions = actions });
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (BindingsGrid.SelectedItem is BindingRow row) _rows.Remove(row);
    }

    private void OnTestStroke(IReadOnlyList<GesturePoint> stroke)
    {
        var result = ProtractorRecognizer.Recognize(
            stroke, _catalog.AllTemplates, _owner.Working.Settings.Sensitivity);
        TestResult.Text = result.IsMatch
            ? $"Recognized: {_catalog.NameOf(result.SymbolId!)}  (score {result.Score:0.00})"
            : $"No match  (best score {result.Score:0.00})";
    }
}
```

- [ ] **Step 3: Register the tab in SettingsWindow**

In `src/SymbolCommander.App/Settings/SettingsWindow.xaml`, insert BEFORE the `<TabItem Header="Actions &amp; General">` line:

```xml
            <TabItem Header="Bindings">
                <local:BindingsTab x:Name="Bindings"/>
            </TabItem>
```

In `SettingsWindow.xaml.cs`:
- in the constructor after `General.Load(this);` add: `Bindings.Load(this);`
- in `ApplyWorking()` after `General.CollectInto(Working);` add: `Bindings.CollectInto(Working);`
- in `Tabs_SelectionChanged` add: `Bindings.Reload();` — but ONLY when the Bindings tab is the one now selected. Replace the method body with:

```csharp
        if (!ReferenceEquals(e.Source, Tabs)) return;
        if (Tabs.SelectedItem is TabItem { Content: BindingsTab bt }) bt.Reload();
        // Task 16 adds the SymbolsTab case here
```

Note: `Reload()` first collects current rows into `Working` (see the `_loaded` guard), so tabbing away and back never loses edits; rows are also collected on every Apply.

- [ ] **Step 4: Build and commit**

```bash
dotnet build 2>&1 | tail -3
git add -A && git commit -m "feat(app): bindings tab with live test-draw area

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: `Build succeeded. 0 Error(s)`.

---

### Task 16: Symbols tab + custom symbol training

**Files:**
- Create: `src/SymbolCommander.App/Settings/SymbolEditorDialog.xaml` + `.xaml.cs`
- Create: `src/SymbolCommander.App/Settings/SymbolsTab.xaml` + `.xaml.cs`
- Modify: `src/SymbolCommander.App/Settings/SettingsWindow.xaml` + `.xaml.cs` — register the tab

**Interfaces:**
- Consumes: `DrawingCanvas` (Task 15), `CustomSymbol`, `SymbolCatalog`, `ProtractorRecognizer` (Tasks 3, 7).
- Produces: complete spec milestone 5. Custom symbols save straight to `ConfigStore` (not the Working clone); collision threshold **0.85**.

- [ ] **Step 1: Write SymbolEditorDialog**

Write `src/SymbolCommander.App/Settings/SymbolEditorDialog.xaml`:

```xml
<Window x:Class="SymbolCommander.App.Settings.SymbolEditorDialog"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:SymbolCommander.App.Settings"
        Title="New Symbol" Width="460" Height="480"
        WindowStartupLocation="CenterOwner">
    <DockPanel Margin="12">
        <StackPanel DockPanel.Dock="Top">
            <TextBlock Text="Name:"/>
            <TextBox x:Name="NameBox" Margin="0,2,0,8"/>
            <TextBlock Text="Draw the symbol 3–5 times (each stroke becomes a training example):"/>
        </StackPanel>
        <StackPanel DockPanel.Dock="Bottom">
            <TextBlock x:Name="StatusLabel" Margin="0,6" FontWeight="SemiBold"/>
            <TextBlock x:Name="ErrorLabel" Foreground="Firebrick" TextWrapping="Wrap" Visibility="Collapsed"/>
            <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,6,0,0">
                <Button Content="Clear examples" Width="110" Margin="4,0" Click="Clear_Click"/>
                <Button Content="Save" Width="80" Margin="4,0" Click="Save_Click"/>
                <Button Content="Cancel" Width="80" Margin="4,0" IsCancel="True"/>
            </StackPanel>
        </StackPanel>
        <local:DrawingCanvas x:Name="Canvas" Margin="0,4"/>
    </DockPanel>
</Window>
```

Write `src/SymbolCommander.App/Settings/SymbolEditorDialog.xaml.cs`:

```csharp
using System.Windows;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Recognition;

namespace SymbolCommander.App.Settings;

public partial class SymbolEditorDialog : Window
{
    private const int MinExamples = 3;
    private const int MaxExamples = 5;
    private const double CollisionThreshold = 0.85;

    private readonly ConfigStore _store;
    private readonly SymbolCatalog _existing;
    private readonly List<List<GesturePoint>> _examples = new();

    public CustomSymbol? Result { get; private set; }

    public SymbolEditorDialog(ConfigStore store)
    {
        InitializeComponent();
        _store = store;
        _existing = new SymbolCatalog(store.LoadCustomSymbols());
        Canvas.StrokeDrawn += OnStroke;
        UpdateStatus();
    }

    private void OnStroke(IReadOnlyList<GesturePoint> stroke)
    {
        if (!StrokePreprocessor.IsValidStroke(stroke))
        {
            StatusLabel.Text = "Stroke too small — draw bigger.";
            return;
        }
        if (_examples.Count >= MaxExamples)
        {
            StatusLabel.Text = $"Already have {MaxExamples} examples. Save, or clear and redraw.";
            return;
        }
        _examples.Add(stroke.ToList());
        UpdateStatus();
    }

    private double Consistency()
    {
        var vectors = _examples.Select(ProtractorRecognizer.ToVector).ToList();
        var scores = new List<double>();
        for (int i = 0; i < vectors.Count; i++)
            for (int j = i + 1; j < vectors.Count; j++)
                scores.Add(ProtractorRecognizer.Similarity(vectors[i], vectors[j]));
        return scores.Count == 0 ? 1 : scores.Average();
    }

    private void UpdateStatus()
    {
        string text = $"Examples: {_examples.Count}/{MaxExamples}";
        if (_examples.Count >= 2)
        {
            double c = Consistency();
            text += $"   Consistency: {c:P0} " + c switch
            {
                >= 0.85 => "— good",
                >= 0.70 => "— okay, try to draw more alike",
                _ => "— inconsistent, clear and redraw",
            };
        }
        StatusLabel.Text = text;
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _examples.Clear();
        Canvas.ClearInk();
        UpdateStatus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorLabel.Visibility = Visibility.Collapsed;
        var name = NameBox.Text.Trim();
        if (name.Length == 0) { ShowError("Give the symbol a name."); return; }
        if (_existing.All.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        { ShowError($"A symbol named \"{name}\" already exists."); return; }
        if (_examples.Count < MinExamples)
        { ShowError($"Draw at least {MinExamples} examples ({_examples.Count} so far)."); return; }

        // collision check: does any example match an existing symbol too closely?
        double worst = 0; string? collidesWith = null;
        foreach (var vec in _examples.Select(ProtractorRecognizer.ToVector))
            foreach (var t in _existing.AllTemplates)
            {
                double s = ProtractorRecognizer.Similarity(t.Vector, vec);
                if (s > worst) { worst = s; collidesWith = _existing.NameOf(t.SymbolId); }
            }
        if (worst >= CollisionThreshold)
        {
            var choice = MessageBox.Show(
                $"This looks very similar to \"{collidesWith}\" (similarity {worst:P0}). " +
                "The two may steal each other's matches. Save anyway?",
                "Symbol Commander", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (choice != MessageBoxResult.Yes) return;
        }

        var symbol = new CustomSymbol { Name = name, Examples = _examples.ToList() };
        _store.SaveCustomSymbol(symbol);
        Result = symbol;
        DialogResult = true;
    }

    private void ShowError(string msg)
    {
        ErrorLabel.Text = msg;
        ErrorLabel.Visibility = Visibility.Visible;
    }
}
```

- [ ] **Step 2: Write SymbolsTab**

Write `src/SymbolCommander.App/Settings/SymbolsTab.xaml`:

```xml
<UserControl x:Class="SymbolCommander.App.Settings.SymbolsTab"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Margin="12">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <GroupBox Grid.Column="0" Header="Built-in symbols" Margin="0,0,6,0">
            <ListBox x:Name="BuiltInList"/>
        </GroupBox>
        <GroupBox Grid.Column="1" Header="Custom symbols" Margin="6,0,0,0">
            <DockPanel>
                <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal" Margin="0,6,0,0">
                    <Button Content="New…" Width="80" Click="New_Click"/>
                    <Button Content="Delete" Width="80" Margin="6,0" Click="Delete_Click"/>
                </StackPanel>
                <ListBox x:Name="CustomList" DisplayMemberPath="Name"/>
            </DockPanel>
        </GroupBox>
    </Grid>
</UserControl>
```

Write `src/SymbolCommander.App/Settings/SymbolsTab.xaml.cs`:

```csharp
using System.Windows;
using System.Windows.Controls;
using SymbolCommander.Core.Config;
using SymbolCommander.Core.Library;

namespace SymbolCommander.App.Settings;

public partial class SymbolsTab : UserControl
{
    private SettingsWindow _owner = null!;

    public SymbolsTab() => InitializeComponent();

    public void Load(SettingsWindow owner)
    {
        _owner = owner;
        foreach (var s in BuiltInSymbols.All) BuiltInList.Items.Add(s.Name);
        Reload();
    }

    public void Reload()
    {
        CustomList.Items.Clear();
        foreach (var s in _owner.Store.LoadCustomSymbols()) CustomList.Items.Add(s);
    }

    public void CollectInto(AppConfig working) { } // customs persist directly via ConfigStore

    private void New_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SymbolEditorDialog(_owner.Store) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
        {
            Reload();
            // make the new symbol usable immediately, without waiting for OK/Apply
            _owner.Coordinator.ApplyConfig(_owner.Coordinator.CurrentConfig, _owner.Store.LoadCustomSymbols());
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (CustomList.SelectedItem is not CustomSymbol symbol) return;
        int used = _owner.Working.Bindings.Count(b => b.SymbolId == symbol.Id);
        var msg = used > 0
            ? $"Delete \"{symbol.Name}\"? {used} binding(s) using it will also be removed."
            : $"Delete \"{symbol.Name}\"?";
        if (MessageBox.Show(msg, "Symbol Commander", MessageBoxButton.YesNo, MessageBoxImage.Question)
            != MessageBoxResult.Yes) return;

        _owner.Working.Bindings.RemoveAll(b => b.SymbolId == symbol.Id);
        _owner.Store.DeleteCustomSymbol(symbol.Id);
        Reload();
        // stop matching the deleted symbol immediately, mirroring New_Click
        _owner.Coordinator.ApplyConfig(_owner.Coordinator.CurrentConfig, _owner.Store.LoadCustomSymbols());
    }
}
```

- [ ] **Step 3: Register the tab in SettingsWindow**

In `src/SymbolCommander.App/Settings/SettingsWindow.xaml`, insert AFTER the Bindings TabItem:

```xml
            <TabItem Header="Symbols">
                <local:SymbolsTab x:Name="Symbols"/>
            </TabItem>
```

In `SettingsWindow.xaml.cs`:
- constructor: add `Symbols.Load(this);` after `Bindings.Load(this);`
- `Tabs_SelectionChanged`: replace the Task 16 comment with `if (Tabs.SelectedItem is TabItem { Content: SymbolsTab st }) st.Reload();`

- [ ] **Step 4: Build, test, commit**

```bash
cd /home/dev1/Rohith/symbol-commander && dotnet build 2>&1 | tail -3 && dotnet test 2>&1 | tail -3
git add -A && git commit -m "feat(app): custom symbol training with consistency and collision checks

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

Expected: build succeeded; all Core tests still pass.

---

### Task 17: Packaging, README, final Windows checklist

**Files:**
- Create: `publish.sh`
- Create: `README.md`

**Interfaces:** consumes everything; produces the shippable artifact.

- [ ] **Step 1: Write publish.sh**

Write `publish.sh` at repo root:

```bash
#!/usr/bin/env bash
# Builds the shippable single-file SymbolCommander.exe (run on Linux or Windows).
set -euo pipefail
cd "$(dirname "$0")"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

dotnet test
dotnet publish src/SymbolCommander.App -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

OUT="src/SymbolCommander.App/bin/Release/net8.0-windows/win-x64/publish/SymbolCommander.exe"
echo
echo "Built: $OUT ($(du -h "$OUT" | cut -f1))"
```

Then: `chmod +x publish.sh`

- [ ] **Step 2: Write README.md**

Write `README.md` at repo root:

```markdown
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

## Build

Requires the .NET 8 SDK. Works on Windows or Linux (cross-build):

    ./publish.sh

Output: `src/SymbolCommander.App/bin/Release/net8.0-windows/win-x64/publish/SymbolCommander.exe`

Run tests only: `dotnet test`

## Architecture

- `src/SymbolCommander.Core` — platform-free: Protractor unistroke recognizer
  (±30° rotation tolerance), trigger state machine, action/config models. All
  unit tests live here.
- `src/SymbolCommander.App` — Windows shell: low-level hooks (WH_MOUSE_LL /
  WH_KEYBOARD_LL) on a watchdog-guarded thread, click-through WPF overlay for
  the ink trail, SendInput-based action executor, tray icon, settings UI.

Design docs: `docs/superpowers/specs/`, plan: `docs/superpowers/plans/`.
```

- [ ] **Step 3: Full build + publish**

```bash
cd /home/dev1/Rohith/symbol-commander && ./publish.sh 2>&1 | tail -6
```

Expected: tests pass, exe path + size printed.

- [ ] **Step 4: Commit and tag**

```bash
git add -A && git commit -m "chore: publish script and README

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
git tag v0.1.0
```

- [ ] **Step 5: FINAL WINDOWS CHECKLIST (user, on the Windows PC)**

Copy the fresh exe over. Everything from the Task 12 checklist must still pass, PLUS:

1. **Settings — Bindings:** change M's action to "Volume up", Apply → drawing M now raises volume (no restart). Add a binding, remove a binding, disable one with the checkbox.
2. **Test area:** drawing in the Bindings tab test box reports matches live and fires nothing.
3. **Actions:** create a Keystroke action by pressing a combo in the recorder box; create a Launch action with Browse; create a Shell action (`notepad`); bind and fire each with a gesture.
4. **Symbols:** train a custom symbol (e.g. a star-like zigzag) with 3 examples — consistency indicator responds; bind it; draw it → action fires. Try training a near-copy of "V" → collision warning appears.
5. **Delete a custom symbol** that has a binding → confirmation mentions the binding; both disappear.
6. **General:** sensitivity slider at 0.95 makes sloppy strokes fail (red fade); at 0.6 they pass. Trail color/thickness changes apply. Hotkey preset change to Ctrl+Shift works.
7. **Start with Windows:** tick, Apply, reboot → app is running (tray icon present). Untick → registry entry gone (`HKCU\...\Run`).
8. **Multi-monitor / DPI (if available):** trail renders under the cursor on a second monitor and on a display scaled ≠100%. Known v1 limitation: on mixed-DPI multi-monitor setups the trail may be slightly offset on non-primary monitors — log exact offsets if seen; fixing beyond that is out of scope for v1.
9. **Corrupt-config recovery:** exit the app, garble `%APPDATA%\SymbolCommander\config.json` (delete half the file), start the app → tray warning appears, defaults work, a `config.json.corrupt-*` backup exists.
10. **Second launch** of the exe while running → settings window opens/activates instead of a second tray icon.

Fix anything the user reports, then this project is done: working recognizer, four action types, custom symbols, trackpad flow, portable exe.

---

## Execution notes

- Tasks 1–7 are fully verifiable here (Linux): TDD each one, run `dotnet test`.
- Tasks 8–17 compile-verify on Linux (`dotnet build`); runtime behavior verifies at the Task 12 and Task 17 user checklists. Batch questions for the user around those two checkpoints.
- If a later task changes Core code, re-run `dotnet test` before committing.
