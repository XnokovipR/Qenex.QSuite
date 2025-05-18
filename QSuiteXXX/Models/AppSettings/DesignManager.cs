using System.Xml.Serialization;

namespace Qenex.QSuite.Models.AppSettings;

[XmlRoot("DesignManager")]
public class DesignManager
{
	[XmlElement("Skin")] public Skin Skin { get; set; }
	[XmlElement("FontSize")] public int FontSize { get; set; }

}