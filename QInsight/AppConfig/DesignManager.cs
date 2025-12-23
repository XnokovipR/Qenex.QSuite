using System.Windows.Media;
using System.Xml.Serialization;

namespace Qenex.QInsight.AppConfig;

[XmlRoot("DesignManager")]
public class DesignManager
{
	[XmlElement("Theme")] public ApplicationTheme AppTheme { get; set; }
	[XmlElement("FontSize")] public int FontSize { get; set; }
	
	[XmlIgnore]
	public Color LightThemeTextColor { get; set; }
	[XmlElement("LightThemeTextColor")]
	public string LightThemeTextColorXml
	{
		get => $"{LightThemeTextColor.R},{LightThemeTextColor.G},{LightThemeTextColor.B}";
		set
		{
			var parts = value.Split(',');
			LightThemeTextColor = Color.FromRgb(
				byte.Parse(parts[0]),
				byte.Parse(parts[1]),
				byte.Parse(parts[2]));
		}
	}
	
	[XmlIgnore]
	public Color LightThemeTextBoxBackgroundColor { get; set; }
	[XmlElement("LightThemeTextBoxBackgroundColor")]
	public string LightThemeTextBoxBackgroundColorXml
	{
		get => $"{LightThemeTextBoxBackgroundColor.R},{LightThemeTextBoxBackgroundColor.G},{LightThemeTextBoxBackgroundColor.B}";
		set
		{
			var parts = value.Split(',');
			LightThemeTextBoxBackgroundColor = Color.FromRgb(
				byte.Parse(parts[0]),
				byte.Parse(parts[1]),
				byte.Parse(parts[2]));
		}
	}
	
	[XmlIgnore]
	public Color LightThemeControlBackgroundColor { get; set; }
	[XmlElement("LightThemeControlBackgroundColor")]
	public string LightThemeControlBackgroundColorXml
	{
		get => $"{LightThemeControlBackgroundColor.R},{LightThemeControlBackgroundColor.G},{LightThemeControlBackgroundColor.B}";
		set
		{
			var parts = value.Split(',');
			LightThemeControlBackgroundColor = Color.FromRgb(
				byte.Parse(parts[0]),
				byte.Parse(parts[1]),
				byte.Parse(parts[2]));
		}
	}
	
	[XmlIgnore]
	public Color DarkThemeTextColor { get; set; }
	[XmlElement("DarkThemeTextColor")] 
	public string DarkThemeTextColorXml
	{
		get => $"{DarkThemeTextColor.R},{DarkThemeTextColor.G},{DarkThemeTextColor.B}";
		set
		{
			var parts = value.Split(',');
			DarkThemeTextColor = Color.FromRgb(
				byte.Parse(parts[0]),
				byte.Parse(parts[1]),
				byte.Parse(parts[2]));
		}
	}
	
	[XmlIgnore]
	public Color DarkThemeTextBoxBackgroundColor { get; set; }
	[XmlElement("DarkThemeTextBoxBackgroundColor")] 
	public string DarkThemeTextBoxBackgroundColorXml
	{
		get => $"{DarkThemeTextBoxBackgroundColor.R},{DarkThemeTextBoxBackgroundColor.G},{DarkThemeTextBoxBackgroundColor.B}";
		set
		{
			var parts = value.Split(',');
			DarkThemeTextBoxBackgroundColor = Color.FromRgb(
				byte.Parse(parts[0]),
				byte.Parse(parts[1]),
				byte.Parse(parts[2]));
		}
	}
	
	[XmlIgnore]
	public Color DarkThemeControlBackgroundColor { get; set; }
	[XmlElement("DarkThemeControlBackgroundColor")] 
	public string DarkThemeControlBackgroundColorXml
	{
		get => $"{DarkThemeControlBackgroundColor.R},{DarkThemeControlBackgroundColor.G},{DarkThemeControlBackgroundColor.B}";
		set
		{
			var parts = value.Split(',');
			DarkThemeControlBackgroundColor = Color.FromRgb(
				byte.Parse(parts[0]),
				byte.Parse(parts[1]),
				byte.Parse(parts[2]));
		}
	}
}