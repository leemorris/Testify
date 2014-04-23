using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Diagnostics;

namespace Leem.Testify
{
    /// <summary>
    /// Convert Level to left margin
    /// Pass a prarameter if you want a unit length other than 19.0.
    /// </summary>
    public class LevelToIndentConverter : IValueConverter
    {
        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            return new Thickness((int)o * c_IndentSize, 0, 0, 0);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private const double c_IndentSize = 19.0;
    }

    public class LevelConverter : DependencyObject, IMultiValueConverter
    {
        public object Convert(
            object[] values, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (values[0] != DependencyProperty.UnsetValue && values[1] != DependencyProperty.UnsetValue)
            {
                            int level = (int)values[0];
            double indent = (double)values[1];
            return indent * level;
            }
            return 0;
        }

        public object[] ConvertBack(
            object value, Type[] targetTypes,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }

    public class DebugDummyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }
    }
}
