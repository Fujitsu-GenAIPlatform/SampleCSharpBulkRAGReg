using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SampleCSharpBulkRAGReg.Converters
{
    internal class RoleToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string && !string.IsNullOrEmpty((string)value))
            {
                var s = value.ToString().Trim().ToLowerInvariant();
                return s == "user" ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}