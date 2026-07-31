using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities
{
    // Visible when the bound string is null/empty; Collapsed otherwise. Used
    // for the search-box placeholder overlays on TopBar and Sidebar, which
    // can't rely on TextBox.Tag/watermark styling elsewhere in the app since
    // those TextBoxes need a real placeholder that disappears once typed.
    public class StringEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var text = value as string;
            return string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
