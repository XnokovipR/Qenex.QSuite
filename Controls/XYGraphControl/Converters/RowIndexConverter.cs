using System.Collections;
using System.Globalization;
using System.Net.Mime;
using System.Windows;
using System.Windows.Data;
using Telerik.Windows.Controls;

namespace Qenex.QSuite.Controls.XYGraphControl.Converters;

public class RowIndexConverter : IMultiValueConverter  // not IValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] == null || values[1] is not IEnumerable items)
            return null;

        // Zobrazovane Id os je 1-based (interni indexy zustavaji 0-based)
        int index = 1;
        foreach (var item in items)
        {
            if (ReferenceEquals(item, values[0]))
                return index.ToString();
            index++;
        }
        return null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}