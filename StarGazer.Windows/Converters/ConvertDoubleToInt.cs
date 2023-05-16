﻿using System.Globalization;
using System.Windows.Data;

namespace StarGazer.UI.Converters
{
    [ValueConversion(typeof(Double), typeof(Int32))]
    public class ConvertDoubleToInt : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Int32))
                return (int)(double)value;

            return (double)(int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)(double)value;
        }
    }
}
