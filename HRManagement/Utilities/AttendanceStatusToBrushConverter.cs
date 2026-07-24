using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities;

public class AttendanceStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value as string switch
        {
            "Present" => "SuccessBgBrush",
            "Late" => "WarningBgBrush",
            "Absent" => "DangerBgBrush",
            "OT" => "OtBgBrush",
            "Weekend" => "WeekendBgBrush",
            "Before Hire Date" => "NeutralBgBrush",
            _ => "CardBackgroundBrush"
        };

        return Application.Current.FindResource(key);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}