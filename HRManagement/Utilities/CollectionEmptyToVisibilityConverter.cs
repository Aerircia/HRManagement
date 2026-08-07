using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities
{
    /// <summary>
    /// Visible when the bound collection is null or empty; Collapsed otherwise.
    /// Used for empty state messages in list views.
    /// </summary>
    public class CollectionEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ICollection collection)
            {
                return collection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (value == null)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
