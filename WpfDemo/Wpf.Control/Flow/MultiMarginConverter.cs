using System;
using System.Windows;
using System.Windows.Data;

namespace Wpf.Control.Flow
{
    //http://stackoverflow.com/questions/6249518/binding-only-part-of-the-margin-property-of-wpf-control
    public class MultiThicknessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new Thickness(System.Convert.ToDouble(values[0]),
                                 System.Convert.ToDouble(values[1]),
                                 System.Convert.ToDouble(values[2]),
                                 System.Convert.ToDouble(values[3]));
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
