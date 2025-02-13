using System.Xml.Serialization;

namespace Qenex.QSuite.Models.AppSettings;

[XmlRoot("Skin")]
public enum Skin
{
	[XmlEnum("Light")] Light,
	[XmlEnum("Dark")] Dark,
}