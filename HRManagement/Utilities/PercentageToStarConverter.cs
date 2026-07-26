using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities;

// Turns a 0-100 percentage into a Star-weighted GridLength, used to size a
// two-column Grid (filled | remainder) as a progress bar without needing
// pixel math or ActualWidth bindings. Pass ConverterParameter="Remainder"
// for the second column so the two always sum to the full width.
public class PercentageToStarConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var percentage = value switch
        {
            double d => d,
            int i => i,
            _ => 0d
        };

        var clamped = Math.Max(0, Math.Min(100, percentage));
        var isRemainder = string.Equals(parameter as string, "Remainder", StringComparison.OrdinalIgnoreCase);

        var weight = isRemainder ? 100 - clamped : clamped;

        // Grid requires a positive star weight; avoid 0 causing layout issues.
        weight = Math.Max(weight, 0.0001);

        return new GridLength(weight, GridUnitType.Star);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
