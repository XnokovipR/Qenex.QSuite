using System.Windows;
using System.Windows.Media;
using Telerik.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;
using Qenex.QInsight.AppConfig;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Qenex.QSuite.Controls.OverridenWinControls;

namespace Qenex.QInsight.Views;

/// <summary>
/// Interaction logic for ShellWindow.xaml
/// </summary>
public partial class ShellWindow : Window
{
	private static readonly int DWMWA_USE_IMMERSIVE_DARK_MODE =
	(Environment.OSVersion.Version.Build >= 18362) ? 20 : 19;

	[DllImport("dwmapi.dll", PreserveSig = true)]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

	internal static AppSettings MainAppSettings = null!;
	internal static Color ForegroundColor;
	internal static Color BackgroundColor;
	internal static Color TextEditorBackgroubndColor;
	
	public static bool IsDarkTheme = false;

	public ShellWindow()
	{
		InitializeComponent();
		
		try
		{
			ProcessAppSettings(AppDataPaths.AppSettingsFile);

			SourceInitialized += WindowSourceInitialized;

			Windows11ThemeSizeHelper.Helper.IsInCompactMode = true;
			ApplyDesignSettings();
		}
		catch (Exception exception)
		{
			Console.WriteLine(exception.Message);
			throw;
		}
	}

	private void WindowSourceInitialized(object sender, EventArgs e)
	{
		ApplyDesignSettings();
	}

	internal static void ApplyDesignSettings()
	{
		if (MainAppSettings?.Design == null)
		{
			return;
		}

		IsDarkTheme = MainAppSettings.Design.AppTheme == ApplicationTheme.Dark;
		ForegroundColor = IsDarkTheme
			? MainAppSettings.Design.DarkThemeTextColor
			: MainAppSettings.Design.LightThemeTextColor;
		BackgroundColor = IsDarkTheme
			? MainAppSettings.Design.DarkThemeControlBackgroundColor
			: MainAppSettings.Design.LightThemeControlBackgroundColor;
		TextEditorBackgroubndColor = IsDarkTheme
			? MainAppSettings.Design.DarkThemeTextBoxBackgroundColor
			: MainAppSettings.Design.LightThemeTextBoxBackgroundColor;

		Windows11Palette.Palette.FontSize = MainAppSettings.Design.FontSize;
		Windows11Palette.LoadPreset(IsDarkTheme ? Windows11Palette.ColorVariation.Dark : Windows11Palette.ColorVariation.Light);

		if (Application.Current.MainWindow is not ShellWindow shellWindow)
		{
			return;
		}

		var textboxStyle = new Style(typeof(QTextBox));
		var textblockStyle = new Style(typeof(QTextBlock));
		var labelStyle = new Style(typeof(QLabel));

		textblockStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(ForegroundColor)));
		textboxStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(ForegroundColor)));
		textboxStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(TextEditorBackgroubndColor)));
		labelStyle.Setters.Add(new Setter(ForegroundProperty, new SolidColorBrush(ForegroundColor)));

		shellWindow.Resources[typeof(QTextBox)] = textboxStyle;
		shellWindow.Resources[typeof(QTextBlock)] = textblockStyle;
		shellWindow.Resources[typeof(QLabel)] = labelStyle;
		shellWindow.Resources["AppBorderBrush"] = IsDarkTheme
			? new SolidColorBrush(Color.FromRgb(50, 50, 50))
			: new SolidColorBrush(Color.FromRgb(200, 200, 200));

		var hwnd = new WindowInteropHelper(shellWindow).Handle;
		if (hwnd != IntPtr.Zero)
		{
			SetImmersiveDarkMode(hwnd, IsDarkTheme);
		}
	}

	private static void SetImmersiveDarkMode(IntPtr hwnd, bool enabled)
	{
		int useDark = enabled ? 1 : 0;
		DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
	}

	private static bool IsDarkThemeEnabled()
	{
		using (var key = Registry.CurrentUser.OpenSubKey(
				   @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false))
		{
			if (key is null) return false; // assume light
			var v = key.GetValue("AppsUseLightTheme");
			return v is int intVal && intVal == 0;
		}
	}
	private void ProcessAppSettings(string filename)
	{
		MainAppSettings = AppSettings.LoadAppSettingsFromFile(filename) ?? AppSettings.GetDefaultAppSettings();

		if (Application.Current.MainWindow != null)
		{
			var win = Application.Current.MainWindow;
			win.Height = MainAppSettings.WinStyle.Height;
			win.Width = MainAppSettings.WinStyle.Width;
			win.WindowState = WindowState.Minimized;
			win.Top = MainAppSettings.WinStyle.Top;
			win.Left = MainAppSettings.WinStyle.Left;

			if (!MainAppSettings.IsAppSettingRead)
			{
				win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
				MainAppSettings.WinStyle.ScreenId = 0;
				MainAppSettings.WinStyle.Height = win.Height;
				MainAppSettings.WinStyle.Width = win.Width;
				//MainAppSettings.WinStyle.WinState;
				MainAppSettings.WinStyle.Top = win.Top;
				MainAppSettings.WinStyle.Left = win.Left;
				MainAppSettings.IsAppSettingRead = true;
			}

			OpenWindowOnDefinedScreen(win, MainAppSettings);
		}
	}

	private void OpenWindowOnDefinedScreen(Window win, AppSettings appSettings)
	{
		var targetScreen = System.Windows.Forms.Screen.AllScreens[appSettings.WinStyle.ScreenId];
		win.Left = targetScreen.Bounds.Left + appSettings.WinStyle.Left;
		win.Top = targetScreen.Bounds.Top + appSettings.WinStyle.Top;
	}
}
