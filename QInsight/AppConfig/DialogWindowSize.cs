using System.Xml.Serialization;

namespace Qenex.QInsight.AppConfig;

/// <summary>
/// Remembered size of a resizable dialog (e.g. Project Configuration). Only the size is kept:
/// dialogs are centred on the application window every time, and the remembered size is
/// clamped to the application window before use so the dialog never grows past it.
/// Zero means "use the built-in default".
/// </summary>
public class DialogWindowSize
{
    [XmlElement("Width")] public double Width { get; set; }
    [XmlElement("Height")] public double Height { get; set; }
}
