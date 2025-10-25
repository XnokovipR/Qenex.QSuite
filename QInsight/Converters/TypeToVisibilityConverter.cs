using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Qenex.QInsight.Converters;

public class TypeNameToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        var itemType = value.GetType();
        var allowedTypeNames = parameter.ToString().Split(',').Select(s => s.Trim());
        
        foreach (var typeName in allowedTypeNames)
        {
            if (itemType.Name == typeName || 
                itemType.FullName == typeName ||
                itemType.BaseType?.Name == typeName ||
                itemType.BaseType?.FullName == typeName)
            {
                return Visibility.Visible;
            }
        }

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}