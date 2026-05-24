using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LTS.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture)
        => value is bool b && !b;
}