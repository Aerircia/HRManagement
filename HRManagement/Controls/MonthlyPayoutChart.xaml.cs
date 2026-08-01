using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HRManagement.Models.Dashboard;
using HRManagement.Services.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace HRManagement.Controls;

/// <summary>
/// "Monthly payout overview" line chart - full payout (salary + overtime +
/// reward, minus deductions) per month for the current employee, from
/// January through the current month. Bind PayoutHistory to
/// DashboardData.MonthlyPayoutHistory / DashboardService.GetMonthlyPayoutHistory().
///
/// Colors are read from the app's current theme brushes (PrimaryBrush,
/// SecondaryTextBrush, DividerBrush, CardBackgroundBrush) rather than
/// hardcoded, because LiveCharts paints are plain SkiaSharp objects, not
/// WPF Brushes - they never see DynamicResource updates on their own.
/// BuildSeries() re-runs whenever ISettingService.ThemeChanged fires (in
/// addition to whenever PayoutHistory changes) so the chart actually
/// repaints after a light/dark toggle instead of staying stuck on
/// whatever palette was active when the control was first constructed.
/// </summary>
public partial class MonthlyPayoutChart : UserControl
{
    private readonly ISettingService? _settingService;
    public static readonly DependencyProperty PayoutHistoryProperty =
        DependencyProperty.Register(
            nameof(PayoutHistory),
            typeof(ObservableCollection<MonthlyPayoutPoint>),
            typeof(MonthlyPayoutChart),
            new PropertyMetadata(new ObservableCollection<MonthlyPayoutPoint>(), OnPayoutHistoryChanged));

    public ObservableCollection<MonthlyPayoutPoint> PayoutHistory
    {
        get => (ObservableCollection<MonthlyPayoutPoint>)GetValue(PayoutHistoryProperty);
        set => SetValue(PayoutHistoryProperty, value);
    }

    public static readonly DependencyProperty SeriesProperty =
        DependencyProperty.Register(
            nameof(Series),
            typeof(ISeries[]),
            typeof(MonthlyPayoutChart),
            new PropertyMetadata(Array.Empty<ISeries>()));

    public ISeries[] Series
    {
        get => (ISeries[])GetValue(SeriesProperty);
        private set => SetValue(SeriesProperty, value);
    }

    public static readonly DependencyProperty XAxesProperty =
        DependencyProperty.Register(
            nameof(XAxes),
            typeof(Axis[]),
            typeof(MonthlyPayoutChart),
            new PropertyMetadata(Array.Empty<Axis>()));

    public Axis[] XAxes
    {
        get => (Axis[])GetValue(XAxesProperty);
        private set => SetValue(XAxesProperty, value);
    }

    public static readonly DependencyProperty YAxesProperty =
        DependencyProperty.Register(
            nameof(YAxes),
            typeof(Axis[]),
            typeof(MonthlyPayoutChart),
            new PropertyMetadata(Array.Empty<Axis>()));

    public Axis[] YAxes
    {
        get => (Axis[])GetValue(YAxesProperty);
        private set => SetValue(YAxesProperty, value);
    }

    public MonthlyPayoutChart()
    {
        InitializeComponent();

        // Resolved via the App-level service locator (same pattern WindowService
        // uses) rather than constructor injection, since this is a plain
        // UserControl instantiated directly in XAML by DashboardView.xaml, not
        // resolved through the DI container.
        _settingService = App.Services?.GetService(typeof(ISettingService)) as ISettingService;

        if (_settingService != null)
            _settingService.ThemeChanged += SettingService_OnThemeChanged;

        Unloaded += (_, _) =>
        {
            if (_settingService != null)
                _settingService.ThemeChanged -= SettingService_OnThemeChanged;
        };

        BuildSeries();
    }

    private void SettingService_OnThemeChanged(object? sender, EventArgs e)
    {
        // Marshal back to the UI thread defensively - SettingService itself
        // raises this synchronously from the same UI-thread call that toggled
        // the theme today, but Dispatcher.Invoke keeps this safe even if that
        // ever changes.
        Dispatcher.Invoke(BuildSeries);
    }

    private static void OnPayoutHistoryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MonthlyPayoutChart)d).BuildSeries();
    }

    // Reads the given brush's current Color from the app's live theme
    // resources and converts it to the SkiaSharp SKColor LiveCharts needs.
    // Falls back to a neutral gray if the key can't be resolved (e.g.
    // design-time), so a missing resource never throws mid-render.
    private static SKColor ResolveSkColor(string resourceKey, byte alpha = 255)
    {
        if (Application.Current?.TryFindResource(resourceKey) is SolidColorBrush brush)
        {
            var c = brush.Color;
            return new SKColor(c.R, c.G, c.B, alpha);
        }

        return new SKColor(0x8A, 0x93, 0xB8, alpha);
    }

    private void BuildSeries()
    {
        var points = PayoutHistory ?? new ObservableCollection<MonthlyPayoutPoint>();

        var primary = ResolveSkColor("PrimaryBrush");
        var primaryFill = ResolveSkColor("PrimaryBrush", 40);
        var axisLabel = ResolveSkColor("SecondaryTextBrush");
        var separator = ResolveSkColor("DividerBrush");
        var geometryFill = ResolveSkColor("CardBackgroundBrush");

        Series = new ISeries[]
        {
            new LineSeries<decimal>
            {
                Values = points.Select(p => p.TotalSalary).ToArray(),
                Name = "Payout",
                Fill = new SolidColorPaint(primaryFill),
                Stroke = new SolidColorPaint(primary) { StrokeThickness = 2.5f },
                GeometryStroke = new SolidColorPaint(primary) { StrokeThickness = 2 },
                // Point markers use the card's own background instead of a
                // hardcoded white, so they read as a "punched hole" outlined
                // in brand color on both a white card and a dark card,
                // rather than a stark white dot that used to stand out
                // against dark cards regardless of theme.
                GeometryFill = new SolidColorPaint(geometryFill),
                GeometrySize = 7,
                LineSmoothness = 0.6
            }
        };

        XAxes = new[]
        {
            new Axis
            {
                Labels = points.Select(p => p.Label).ToArray(),
                LabelsPaint = new SolidColorPaint(axisLabel),
                TextSize = 12,
                ShowSeparatorLines = false
            }
        };

        YAxes = new[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(axisLabel),
                TextSize = 12,
                MinLimit = 0,
                Labeler = value => value.ToString("N0"),
                SeparatorsPaint = new SolidColorPaint(separator) { StrokeThickness = 1 }
            }
        };
    }
}