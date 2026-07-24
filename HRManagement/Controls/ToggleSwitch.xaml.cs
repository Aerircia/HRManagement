using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HRManagement.Controls;

public partial class ToggleSwitch : UserControl
{
    public ToggleSwitch()
    {
        InitializeComponent();
        UpdateVisual();
    }

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(ToggleSwitch),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnCheckedChanged));

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    private static void OnCheckedChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        ((ToggleSwitch)d).UpdateVisual();
    }

    private void Grid_MouseLeftButtonUp(object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        IsChecked = !IsChecked;
    }

    private void UpdateVisual()
    {
        if (IsChecked)
        {
            BackgroundBorder.Background =
                (Brush)Application.Current.Resources["AccentBrush"];

            Thumb.HorizontalAlignment = HorizontalAlignment.Right;
        }
        else
        {
            BackgroundBorder.Background =
                new SolidColorBrush(Color.FromRgb(210, 210, 210));

            Thumb.HorizontalAlignment = HorizontalAlignment.Left;
        }
    }
}