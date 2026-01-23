using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SampleCSharpBulkRAGReg.Converters
{
    public class DoubleToGridLengthConverter : IValueConverter
    {
        public double Min { get; set; } = 1;
        public double Max { get; set; } = 110;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double doubleValue = 0;
            if (value is double)
            {
                doubleValue = Math.Max(Min, Math.Min(Max, (double)value));
                return new GridLength(doubleValue);
            }
            else
            {
                return new GridLength(Min); 
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GridLength)
            {
                return Math.Max(Min, Math.Min(Max, ((GridLength)value).Value));
            }
            else
            {
                return Min;
            }
        }
    }
}