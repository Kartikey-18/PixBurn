using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PixBurn.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Handle both bool and object (null check)
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;

        return value is not null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}
