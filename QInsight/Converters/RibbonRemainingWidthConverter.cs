using System.Globalization;
using System.Windows.Data;

namespace Qenex.QInsight.Converters;

public class RibbonRemainingWidthConverter : IValueConverter
{
    public double ReservedWidth { get; set; } = 360;
    public double MinWidth { get; set; } = 320;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double width || double.IsNaN(width) || double.IsInfinity(width))
        {
            return MinWidth;
        }

        return Math.Max(MinWidth, width - ReservedWidth);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
