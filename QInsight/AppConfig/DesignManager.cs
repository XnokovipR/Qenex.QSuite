using System.Xml.Serialization;

namespace Qenex.QInsight.AppConfig;

[XmlRoot("DesignManager")]
public class DesignManager
{
	[XmlElement("Theme")] public ApplicationTheme AppTheme { get; set; }
	[XmlElement("FontSize")] public int FontSize { get; set; }

}