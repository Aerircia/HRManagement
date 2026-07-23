using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities
{
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
}
