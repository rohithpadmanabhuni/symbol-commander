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
