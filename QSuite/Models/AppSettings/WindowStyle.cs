using System.Windows;
using System.Xml.Serialization;

namespace Qenex.QSuite.Models.AppSettings;

[XmlRoot("WindowStyle")]
public class WindowStyle
{
    [XmlElement("Width")] public int Width { get; set; }
    [XmlElement("Height")] public int Height { get; set; }
    [XmlElement("WindowState")] public WindowState WinState { get; set; }
    
}