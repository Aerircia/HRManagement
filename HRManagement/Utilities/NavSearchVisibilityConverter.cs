using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities
{
    // values[0] = SidebarViewModel.SearchText, values[1] = this nav item's
    // own label (passed via a Binding with ElementName / static string in
    // Sidebar.xaml). Empty search text always shows every item; otherwise
    // a case-insensitive substring match on the label decides visibility.
    public class NavSearchVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return Visibility.Visible;

            var searchText = values[0] as string;
            var label = values[1] as string ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
                return Visibility.Visible;

            return label.Contains(searchText.Trim(), StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
