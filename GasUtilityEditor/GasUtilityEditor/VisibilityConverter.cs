using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GasUtilityEditor;

public class VisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isReverse = parameter is string paramString && paramString.Equals("Reverse");
        var isVisible = value is bool boolValue ? boolValue :
            value is string valueString && !string.IsNullOrEmpty(valueString);
        if (isReverse)
            isVisible = !isVisible;
        return isVisible ? Visibility.Visible : Visibility.Collapsed; ;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}