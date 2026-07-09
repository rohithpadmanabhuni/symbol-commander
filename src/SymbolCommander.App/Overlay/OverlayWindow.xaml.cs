using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
// UseWindowsForms pulls System.Drawing into implicit usings; alias the collisions to WPF types.
using Point = System.Windows.Point;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

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
