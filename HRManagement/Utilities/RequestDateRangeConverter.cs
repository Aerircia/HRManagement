using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace HRManagement.Utilities
{
    public class RequestDateRangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Length < 3)
                return "-";

            var requestType = values[0] as string;
            var startDate = values[1] as DateTime?;
            var endDate = values[2] as DateTime?;

            return requestType switch
            {
                "Day Off" when startDate != null && endDate != null =>
                    $"{startDate:MMM dd} - {endDate:MMM dd, yyyy}",

                "Resignation" when endDate != null =>
                    $"Last day: {endDate:MMM dd, yyyy}",

                "OT Request" when startDate != null && endDate != null =>
                    $"{startDate:MMM dd, yyyy} {startDate:HH:mm} - {endDate:HH:mm}",

                _ => "-"
            };
        }

        public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
