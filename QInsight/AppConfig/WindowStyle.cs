using System.Windows;
using System.Xml.Serialization;

namespace Qenex.QInsight.AppConfig;

[XmlRoot("WindowStyle")]
public class WindowStyle
{
    [XmlElement("ScreenId")] public int ScreenId { get; set; }
    [XmlElement("Width")] public double Width { get; set; }
    [XmlElement("Height")] public double Height { get; set; }
    [XmlElement("Top")] public double Top { get; set; }
    [XmlElement("Left")] public double Left { get; set; }
    [XmlElement("WindowState")] public WindowState WinState { get; set; }
    
}