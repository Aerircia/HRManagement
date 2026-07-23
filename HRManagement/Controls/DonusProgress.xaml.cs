using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HRManagement.Controls
{

    public partial class DonutProgress : UserControl
    {
        private const double Size = 120;
        private const double Center = Size / 2;

        public DonutProgress()
        {
            InitializeComponent();
            Loaded += (_, __) => Redraw();
        }

        public double Percentage
        {
            get => (double)GetValue(PercentageProperty);
            set => SetValue(PercentageProperty, value);
        }

        public static readonly DependencyProperty PercentageProperty =
            DependencyProperty.Register(nameof(Percentage), typeof(double), typeof(DonutProgress),
                new PropertyMetadata(0d, OnVisualPropertyChanged));

        public double Thickness
        {
            get => (double)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        public static readonly DependencyProperty ThicknessProperty =
            DependencyProperty.Register(nameof(Thickness), typeof(double), typeof(DonutProgress),
                new PropertyMetadata(12d, OnVisualPropertyChanged));

        public Brush RingBrush
        {
            get => (Brush)GetValue(RingBrushProperty);
            set => SetValue(RingBrushProperty, value);
        }

        public static readonly DependencyProperty RingBrushProperty =
            DependencyProperty.Register(nameof(RingBrush), typeof(Brush), typeof(DonutProgress),
                new PropertyMetadata(Brushes.DodgerBlue));

        public Brush TrackBrush
        {
            get => (Brush)GetValue(TrackBrushProperty);
            set => SetValue(TrackBrushProperty, value);
        }

        public static readonly DependencyProperty TrackBrushProperty =
            DependencyProperty.Register(nameof(TrackBrush), typeof(Brush), typeof(DonutProgress),
                new PropertyMetadata(Brushes.LightGray));

        public object CenterContent
        {
            get => GetValue(CenterContentProperty);
            set => SetValue(CenterContentProperty, value);
        }

        public static readonly DependencyProperty CenterContentProperty =
            DependencyProperty.Register(nameof(CenterContent), typeof(object), typeof(DonutProgress));

        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DonutProgress donut)
                donut.Redraw();
        }

        private void Redraw()
        {
            if (TrackPath == null || ProgressPath == null)
                return;

            double radius = Center - (Thickness / 2);

            TrackPath.Data = BuildRingGeometry(radius, 0, 359.999);

            double clamped = Math.Max(0, Math.Min(100, Percentage));
            double sweepAngle = 360d * (clamped / 100d);

            ProgressPath.Data = sweepAngle <= 0
                ? Geometry.Empty
                : BuildRingGeometry(radius, 0, sweepAngle);
        }

        private Geometry BuildRingGeometry(double radius, double startAngle, double sweepAngle)
        {
            Point start = PointOnCircle(radius, startAngle);
            Point end = PointOnCircle(radius, startAngle + sweepAngle);
            bool isLargeArc = sweepAngle > 180;

            var figure = new PathFigure { StartPoint = start, IsClosed = false };
            figure.Segments.Add(new ArcSegment(
                point: end,
                size: new Size(radius, radius),
                rotationAngle: 0,
                isLargeArc: isLargeArc,
                sweepDirection: SweepDirection.Clockwise,
                isStroked: true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

        private static Point PointOnCircle(double radius, double angleDegrees)
        {
          
            double radians = (Math.PI / 180) * (angleDegrees - 90);
            return new Point(
                Center + radius * Math.Cos(radians),
                Center + radius * Math.Sin(radians));
        }
    }
}
