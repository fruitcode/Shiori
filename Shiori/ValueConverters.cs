using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Data;
using Shiori.Playlist;

namespace Shiori
{
    public class LeftPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new GridLength(System.Convert.ToDouble(value), GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RightPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new GridLength(100 - System.Convert.ToDouble(value), GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class WidthPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ListeningProgressRange r = (ListeningProgressRange)value;
            return new GridLength(r.EndPercent - r.StartPercent, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
