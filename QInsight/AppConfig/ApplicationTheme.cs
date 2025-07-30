using System.Xml.Serialization;

namespace Qenex.QInsight.AppConfig;

[XmlRoot("Theme")]
public enum ApplicationTheme
{
	[XmlEnum("Light")] Light,
	[XmlEnum("Dark")] Dark,
}