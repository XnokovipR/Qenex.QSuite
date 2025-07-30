using System.IO;
using System.Xml.Serialization;
using Qenex.QLibs.XmlInOut;

namespace Qenex.QInsight.AppConfig;

[XmlRoot("AppSettings")]
public class AppSettings
{
	#region Constructors

	private AppSettings() {}

	#endregion

	#region Properties

	[XmlElement("IsAppSettingRead")] public bool IsAppSettingRead { get; set; }
	[XmlElement("WindowStyle")] public WindowStyle WinStyle { get; set; } = null!;
	[XmlElement("DesignManager")] public DesignManager Design { get; set; } = null!;

	#endregion

	#region Public methods

	public static AppSettings LoadAppSettingsFromFile(string fileName)
	{
		AppSettings settings;
		try
		{
			settings = XmlInOut<AppSettings>.LoadFromFile(fileName);
			
		}
		catch (FileNotFoundException)
		{
			settings = GetDefaultAppSettings();
		}
		
		return settings;
	}

	public static AppSettings GetDefaultAppSettings()
	{
		return new AppSettings()
		{
			IsAppSettingRead = false,
			
			Design = new DesignManager()
			{
				AppTheme = ApplicationTheme.Light,
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
		XmlInOut<AppSettings>.SaveToFile(fileName, settings);
	}

	#endregion
}

