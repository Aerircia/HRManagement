using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace HRManagement.Utilities
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var resourceKey = value as string switch
            {
                "Approved" => "SuccessBrush",
                "Rejected" => "DangerBrush",
                "Pending" => "WarningBrush",
                _ => "SecondaryTextBrush"
            };

            return Application.Current.FindResource(resourceKey);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
