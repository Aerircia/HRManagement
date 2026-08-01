using System.Globalization;
using System.Windows.Data;

namespace HRManagement.Utilities;

// Turns a full name into up-to-two-letter initials (e.g. "Sarah Saad" -> "SS",
// "Ali" -> "A") for use in avatar-badge placeholders (Manage Profiles table).
public class InitialsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value as string;

        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return string.Empty;

        if (parts.Length == 1)
            return parts[0][..1].ToUpper(culture);

        return (parts[0][..1] + parts[^1][..1]).ToUpper(culture);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
