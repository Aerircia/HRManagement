using HRManagement.ViewModels;
using System.Windows;

namespace HRManagement.Views;

public partial class MainWindow : Window
{
    private const double CornerRadiusValue = 14;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        SizeChanged += MainWindow_SizeChanged;
        StateChanged += MainWindow_StateChanged;
    }

    // Grid.Clip (RectangleGeometry) doesn't auto-track ActualWidth/Height,
    // so the rounded-corner clip region is kept in sync here whenever the
    // window resizes (including the resize WPF performs internally when
    // toggling WindowState).
    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ContentClipGeometry.Rect = new Rect(0, 0, ActualWidth, ActualHeight);
    }

    // A maximized window fills the work area with no desktop visible around
    // it, so rounded corners would just clip real content instead of
    // revealing the background - standard behavior is to square off the
    // corners while maximized and restore rounding when back to Normal.
    // MaxWidth/MaxHeight are also clamped to the work area here because
    // AllowsTransparency="True" windows otherwise bleed a few pixels past
    // the visible screen edge when maximized (a well-known WPF quirk).
    private void MainWindow_StateChanged(object sender, System.EventArgs e)
    {
        var isMaximized = WindowState == WindowState.Maximized;
        var radius = isMaximized ? 0d : CornerRadiusValue;

        RootBorder.CornerRadius = new CornerRadius(radius);
        ContentClipGeometry.RadiusX = radius;
        ContentClipGeometry.RadiusY = radius;

        if (isMaximized)
        {
            MaxWidth = SystemParameters.WorkArea.Width;
            MaxHeight = SystemParameters.WorkArea.Height;
        }
        else
        {
            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;
        }
    }
}
