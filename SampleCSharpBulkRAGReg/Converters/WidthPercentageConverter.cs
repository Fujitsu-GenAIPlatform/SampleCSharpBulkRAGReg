using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SampleCSharpBulkRAGReg.Converters
{
    public class WidthPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double width))
                return DependencyProperty.UnsetValue;

            double percent = 1.0;
            if (parameter != null)
            {
                var s = parameter as string;
                if (s != null)
                {
                    if (!double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out percent))
                    {
                        percent = 1.0;
                    }
                }
                else if (parameter is double d)
                {
                    percent = d;
                }
            }

            return width * percent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
