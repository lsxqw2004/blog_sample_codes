using System;
using System.Globalization;
using System.Windows.Data;

namespace Wpf.Control.Flow
{
    // stackoverflow.com/questions/2186933/wpf-animation-binding-to-the-to-attribute-of-storyboard-animation
    public class MultiWidthAnimationConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double result = 1.0;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is double)
                    result *= (double)values[i];
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new Exception("Not implemented");
        }
    }
}
