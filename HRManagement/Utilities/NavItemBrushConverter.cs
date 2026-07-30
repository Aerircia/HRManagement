using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HRManagement.Utilities
{
    // Compares SidebarViewModel.CurrentPageType (values[0]) against the
    // individual nav Button's own page Type (values[1], bound via
    // CommandParameter) to resolve the selected-pill Background/Foreground.
    // Mirrors FilterChipBrushConverter's pattern for the status filter chips.
    public class NavItemBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return Application.Current.FindResource("SidebarTextBrush");

            var isSelected = values[0] is Type current && values[1] is Type own && current == own;
            var mode = parameter as string;

            if (mode == "Background")
                return isSelected ? Application.Current.FindResource("SidebarSelectedBrush") : Brushes.Transparent;

            // Foreground (default)
            return Application.Current.FindResource(isSelected ? "SidebarSelectedTextBrush" : "SidebarTextBrush");
        }

        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
