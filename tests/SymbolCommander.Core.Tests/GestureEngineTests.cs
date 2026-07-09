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
