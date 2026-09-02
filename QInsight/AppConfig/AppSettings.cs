using System.IO;
using System.Windows.Media;
using System.Xml.Serialization;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.Scripting.ScriptingEngine;

namespace Qenex.QInsight.AppConfig;

[XmlRoot("AppSettings")]
public class AppSettings
{
	#region Constructors

	private AppSettings() {}

	#endregion

	#region Constants

	public const double ProjectConfigurationDefaultWidth = 1200;
	public const double ProjectConfigurationDefaultHeight = 780;

	#endregion

	#region Properties

	[XmlElement("IsAppSettingRead")] public bool IsAppSettingRead { get; set; }
	[XmlElement("WindowStyle")] public WindowStyle WinStyle { get; set; } = null!;
	[XmlElement("DesignManager")] public DesignManager Design { get; set; } = null!;
	[XmlElement("ScriptEngineSettings")] public ScriptEngineSettings ScriptEngine { get; set; } = null!;
	// Last size the user gave the Project Configuration dialog; restored on the next open,
	// clamped to the application window. Missing in older files -> defaults (EnsureDefaults).
	[XmlElement("ProjectConfigurationWindow")] public DialogWindowSize ProjectConfigurationWindow { get; set; } = null!;
	// Last directory the project Open/Save As dialog was used in; restored across sessions.
	[XmlElement("LastProjectDirectory")] public string LastProjectDirectory { get; set; } = string.Empty;
	// Show Trace/Debug rows in the Logs panel (protocol/driver diagnostic detail).
	// Toggled in General preferences; default off — users see Info and above.
	[XmlElement("ShowDebugLogMessages")] public bool ShowDebugLogMessages { get; set; }

	#endregion

	#region Public methods

	public static AppSettings LoadAppSettingsFromFile(string fileName)
	{
		AppSettings settings;
		try
		{
			settings = EnsureDefaults(XmlInOut<AppSettings>.LoadFromFile(fileName));
			
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
				FontSize = 12,
				LightThemeTextColor = Color.FromRgb(0,0,0),
				LightThemeTextBoxBackgroundColor = Color.FromRgb(220,220,220),
				LightThemeControlBackgroundColor = Color.FromRgb(248,248, 248),
				DarkThemeTextColor = Color.FromRgb(220,220,220),
				DarkThemeTextBoxBackgroundColor = Color.FromRgb(30,30,30),
				DarkThemeControlBackgroundColor = Color.FromRgb(40,40,40)
			},
			WinStyle = new WindowStyle()
			{
				ScreenId = 0,
				Height = 900,
				Width = 1400,
				Left = 200,
				Top = 200,
				WinState = System.Windows.WindowState.Normal
			},
			ScriptEngine = new ScriptEngineSettings()
			{
				PythonDllPath = string.Empty
			},
			ProjectConfigurationWindow = new DialogWindowSize()
			{
				Width = ProjectConfigurationDefaultWidth,
				Height = ProjectConfigurationDefaultHeight
			}
		};
	}

	public static void SaveAppSettingsToFile(string fileName, AppSettings settings)
	{
		XmlInOut<AppSettings>.SaveToFile(fileName, settings);
	}

	private static AppSettings EnsureDefaults(AppSettings settings)
	{
		var defaults = GetDefaultAppSettings();
		settings.WinStyle ??= defaults.WinStyle;
		settings.Design ??= defaults.Design;
		settings.ScriptEngine ??= defaults.ScriptEngine;
		settings.ProjectConfigurationWindow ??= defaults.ProjectConfigurationWindow;
		settings.LastProjectDirectory ??= string.Empty;
		return settings;
	}

	#endregion
}

