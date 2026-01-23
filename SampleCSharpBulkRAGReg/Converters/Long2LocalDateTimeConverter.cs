using System;
using System.Globalization;
using System.Windows.Data;

namespace SampleCSharpBulkRAGReg.Converters
{
    public class Long2LocalDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds((long)value).ToLocalTime().DateTime;
            }
            else
            {
                return DateTime.UtcNow.ToLocalTime();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
