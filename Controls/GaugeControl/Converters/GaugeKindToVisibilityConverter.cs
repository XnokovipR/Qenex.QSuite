using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Qenex.QSuite.Controls.GaugeControl.Converters;

/// <summary>Zviditelni prvek pokud aktualni GaugeKind odpovida ConverterParameter (nazev kindu).</summary>
public class GaugeKindToVisibilityConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal)
			? Visibility.Visible
			: Visibility.Collapsed;

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> Binding.DoNothing;
}
