using System.Globalization;
using System.Windows.Data;

namespace Qenex.QInsight.Converters;

/// <summary>
/// Width proportional to the font size (width = FontSize * Factor), so an element sized for
/// a fixed text (e.g. a "00:00.000" seek box) scales with the user's font setting instead of
/// clipping at larger fonts.
/// </summary>
public class FontSizeToWidthConverter : IValueConverter
{
    public double Factor { get; set; } = 7.4;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is double fontSize && !double.IsNaN(fontSize) && fontSize > 0
            ? fontSize * Factor
            : 12.0 * Factor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
