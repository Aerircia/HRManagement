using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities
{
    // Converts SidebarViewModel.IsCollapsed into the pixel width the host
    // Grid.ColumnDefinition (MainWindow.xaml) should use for the sidebar
    // column, so collapsing the sidebar also shrinks the layout column it
    // lives in instead of just clipping its own content.
    public class CollapsedToWidthConverter : IValueConverter
    {
        public const double ExpandedWidth = 240;
        public const double CollapsedWidth = 76;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isCollapsed = value switch
            {
                bool b => b,
                _ => false
            };

            return new GridLength(isCollapsed ? CollapsedWidth : ExpandedWidth);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
