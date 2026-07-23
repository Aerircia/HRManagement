using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities
{

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isTrue = value switch
            {
                bool b => b,
                string s => !string.IsNullOrEmpty(s),
                null => false,
                _ => true
            };

            return isTrue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Visible;

            return false;
        }
    }
}
