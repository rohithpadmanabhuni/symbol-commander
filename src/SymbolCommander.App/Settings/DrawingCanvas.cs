using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SymbolCommander.Core.Recognition;
// UseWindowsForms pulls System.Drawing in; alias the collisions to WPF types.
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;

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
