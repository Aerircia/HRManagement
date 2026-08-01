using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HRManagement.Models.Dashboard;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace HRManagement.Controls;

/// <summary>
/// Weekly hours-worked line chart (LiveCharts2). Bind WeeklyHours to
/// DashboardData.WeeklyHours / DashboardService.GetWeeklyHours().
/// Rebuilds the LiveCharts Series/XAxes whenever the source collection
/// is replaced (e.g. after a dashboard reload following check-in/out).
/// </summary>
public partial class AttendanceChart : UserControl
{
    public static readonly DependencyProperty WeeklyHoursProperty =
        DependencyProperty.Register(
            nameof(WeeklyHours),
            typeof(ObservableCollection<WeeklyHourPoint>),
            typeof(AttendanceChart),
            new PropertyMetadata(new ObservableCollection<WeeklyHourPoint>(), OnWeeklyHoursChanged));

    public ObservableCollection<WeeklyHourPoint> WeeklyHours
    {
        get => (ObservableCollection<WeeklyHourPoint>)GetValue(WeeklyHoursProperty);
        set => SetValue(WeeklyHoursProperty, value);
    }

    public static readonly DependencyProperty SeriesProperty =
        DependencyProperty.Register(
            nameof(Series),
            typeof(ISeries[]),
            typeof(AttendanceChart),
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
            typeof(AttendanceChart),
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
            typeof(AttendanceChart),
            new PropertyMetadata(Array.Empty<Axis>()));

    public Axis[] YAxes
    {
        get => (Axis[])GetValue(YAxesProperty);
        private set => SetValue(YAxesProperty, value);
    }

    public AttendanceChart()
    {
        InitializeComponent();
        BuildSeries();
    }

    private static void OnWeeklyHoursChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((AttendanceChart)d).BuildSeries();
    }

    private void BuildSeries()
    {
        var points = WeeklyHours ?? new ObservableCollection<WeeklyHourPoint>();

        Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = points.Select(p => p.Hours).ToArray(),
                Name = "Hours",
                Fill = new SolidColorPaint(new SKColor(0x3E, 0x63, 0xDD, 40)),
                Stroke = new SolidColorPaint(new SKColor(0x3E, 0x63, 0xDD)) { StrokeThickness = 2.5f },
                GeometryStroke = new SolidColorPaint(new SKColor(0x3E, 0x63, 0xDD)) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White),
                GeometrySize = 7,
                LineSmoothness = 0.6
            }
        };

        XAxes = new[]
        {
            new Axis
            {
                Labels = points.Select(p => p.Label).ToArray(),
                LabelsPaint = new SolidColorPaint(new SKColor(0x8A, 0x93, 0xB8)),
                TextSize = 12,
                ShowSeparatorLines = false
            }
        };

        YAxes = new[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(new SKColor(0x8A, 0x93, 0xB8)),
                TextSize = 12,
                MinLimit = 0,
                SeparatorsPaint = new SolidColorPaint(new SKColor(0xED, 0xF0, 0xF8)) { StrokeThickness = 1 }
            }
        };
    }
}