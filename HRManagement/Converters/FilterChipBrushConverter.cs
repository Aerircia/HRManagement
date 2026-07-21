using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Converters;

// Picks the selected/unselected brush for a status filter chip.
// ConverterParameter is "Background" or "Foreground".
public class FilterChipBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return Application.Current.FindResource("CardBackgroundBrush");

        var isSelected = (values[0] as string) == (values[1] as string);
        var mode = parameter as string;

        if (mode == "Foreground")
            return Application.Current.FindResource(isSelected ? "WhiteBrush" : "PrimaryTextBrush");

        return Application.Current.FindResource(isSelected ? "PrimaryBrush" : "CardBackgroundBrush");
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
