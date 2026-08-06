using System.Globalization;
using System.Windows.Data;
using FontAwesome.Sharp;
using HRManagement.Models;

namespace HRManagement.Utilities;

/// <summary>
/// Maps an Announcement to a display "type" (fa:IconBlock icon + accent brush
/// key) based on simple keyword matching over Title + Content. Used by
/// AnnouncementCard's optional icon column (ShowTypeIcon="True") so different
/// announcement kinds (maintenance, payment, lease/HR, general) get a
/// recognizable badge instead of a plain date.
///
/// Returns IconChar (not a plain string) because fa:IconBlock.Icon is typed
/// IconChar - binding a bare string here would fail at runtime.
///
/// Keyword sets are intentionally small and conservative - this is a visual
/// nicety, not a classifier, so ambiguous/no-match text always falls back to
/// a generic "bullhorn" announcement icon rather than guessing wrong.
/// </summary>
public class AnnouncementTypeConverter : IValueConverter
{
    private static readonly (string[] Keywords, IconChar Icon, string BrushKey)[] Rules =
    [
        (["maintenance", "repair", "faucet", "fix"], IconChar.Wrench, "PrimaryBrush"),
        (["payment", "rent", "invoice", "paid", "salary", "payslip"], IconChar.MoneyBill1Wave, "SuccessBrush"),
        (["lease", "renewal", "expire", "contract"], IconChar.FileContract, "WarningBrush"),
        (["holiday", "leave", "vacation", "day off"], IconChar.Umbrella, "OtBrush"),
        (["meeting", "event", "schedule"], IconChar.CalendarDay, "PrimaryBrush"),
        (["policy", "hr", "handbook"], IconChar.Building, "PrimaryBrush"),
    ];

    private const IconChar DefaultIcon = IconChar.Bullhorn;
    private const string DefaultBrushKey = "PrimaryBrush";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConverterParameter="Brush" returns the brush key instead of the icon,
        // so one converter instance serves both the icon and its accent color.
        if (string.Equals(parameter as string, "Brush", StringComparison.OrdinalIgnoreCase))
            return Resolve(value).BrushKey;

        return Resolve(value).Icon;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static (IconChar Icon, string BrushKey) Resolve(object? value)
    {
        if (value is not Announcement announcement)
            return (DefaultIcon, DefaultBrushKey);

        var haystack = $"{announcement.Title} {announcement.Content}".ToLowerInvariant();

        foreach (var (keywords, icon, brushKey) in Rules)
        {
            if (keywords.Any(haystack.Contains))
                return (icon, brushKey);
        }

        return (DefaultIcon, DefaultBrushKey);
    }
}
