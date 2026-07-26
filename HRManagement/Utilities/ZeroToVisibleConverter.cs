using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities;

// Shows the element only when the bound count is 0 (empty-state text).
// Pass ConverterParameter="Inverse" to instead show only when count > 0
// (e.g. hiding the empty-state's sibling list container).
public class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var count = value switch
        {
            int i => i,
            _ => 0
        };

        var inverse = string.Equals(parameter as string, "Inverse", StringComparison.OrdinalIgnoreCase);
        var isZero = count == 0;

        var visible = inverse ? !isZero : isZero;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
