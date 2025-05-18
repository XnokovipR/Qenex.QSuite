using System.IO;
using System.Xml.Serialization;
using Qenex.QLibs.XmlInOut;

namespace Qenex.QSuite.Models.AppSettings;

[XmlRoot("AppSettings")]
public class AppSettings
{
	#region Constructors

	private AppSettings()
	{}

	#endregion

	#region Properties

	[XmlElement("IsAppSettingRead")] public bool IsAppSettingRead { get; set; }
	[XmlElement("WindowStyle")] public WindowStyle WinStyle { get; set; }
	[XmlElement("DesignManager")] public DesignManager Design { get; set; }

	#endregion

	#region Public methods

	public static AppSettings? LoadAppSettingsFromFile(string fileName)
	{
		try
		{
			var settings = XmlInOut<AppSettings>.LoadFromFile(fileName);
			return settings;
		}
		catch (FileNotFoundException e)
		{
			return null;
		}
	}

	public static AppSettings GetDefaultAppSettings()
	{
		return new AppSettings()
		{
			IsAppSettingRead = false,
			
			Design = new DesignManager()
			{
				Skin = Skin.Light,
				FontSize = 12
			},
			WinStyle = new WindowStyle()
			{
				ScreenId = 0,
				Height = 900,
				Width = 1400,
				Left = 200,
				Top = 200,
				WinState = System.Windows.WindowState.Normal
			}
			
		};
	}

	public static void SaveAppSettingsToFile(string fileName, AppSettings settings)
	{
		
		try
		{
			XmlInOut<AppSettings>.SaveToFile(fileName, settings);
		}
		catch (Exception e)
		{
			
		}
	}

	#endregion
}

