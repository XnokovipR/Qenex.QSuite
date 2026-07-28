using System.Globalization;
using System.Windows.Data;

namespace Qenex.QInsight.Converters;

/// <summary>
/// Remaining ribbon width for a group with the reserve scaled by font size:
/// result = max(MinWidth * scale, totalWidth - ReservedWidth * scale), scale = FontSize / ReferenceFontSize.
/// The reserve (width of the preceding groups) grows with the font, but is derived from the
/// font setting only — deliberately not from a measured ActualWidth, which would feed the
/// ribbon's own layout back into itself and let a group collapse on the first pass.
/// </summary>
public class FontScaledRibbonWidthConverter : IMultiValueConverter
{
    /// <summary>Reserve for the preceding group(s), measured at <see cref="ReferenceFontSize"/>.</summary>
    public double ReservedWidth { get; set; } = 170;

    public double ReferenceFontSize { get; set; } = 12;

    public double MinWidth { get; set; } = 320;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var scale = values.Length > 1
                    && values[1] is double fontSize
                    && !double.IsNaN(fontSize)
                    && fontSize > 0
                    && ReferenceFontSize > 0
            ? fontSize / ReferenceFontSize
            : 1.0;

        if (values.Length < 1 || values[0] is not double totalWidth
            || double.IsNaN(totalWidth) || double.IsInfinity(totalWidth))
        {
            return MinWidth * scale;
        }

        return Math.Max(MinWidth * scale, totalWidth - ReservedWidth * scale);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
