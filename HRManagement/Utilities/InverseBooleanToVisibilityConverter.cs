using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities
{
    // Same semantics as BooleanToVisibilityConverter but inverted: True -> Collapsed,
    // False -> Visible. Used to hide sidebar text labels/section headers/search box
    // when IsCollapsed is true, without needing a ConverterParameter on every binding.
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isTrue = value switch
            {
                bool b => b,
                _ => false
            };

            return isTrue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility != Visibility.Visible;

            return false;
        }
    }
}
