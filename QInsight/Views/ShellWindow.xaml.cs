using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Telerik.Windows.Controls;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Qenex.QInsight.Views
{
	/// <summary>
	/// Interaction logic for ShellWindow.xaml
	/// </summary>
	public partial class ShellWindow : Window
	{
		private static readonly int DWMWA_USE_IMMERSIVE_DARK_MODE =
			(Environment.OSVersion.Version.Build >= 18362) ? 20 : 19;

		[DllImport("dwmapi.dll", PreserveSig = true)]
		private static extern int DwmSetWindowAttribute(
			IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		public ShellWindow()
		{
			InitializeComponent();
			SourceInitialized += WindowSourceInitialized;
		}

		private void WindowSourceInitialized(object sender, EventArgs e)
		{
			var hwnd = new WindowInteropHelper(this).Handle;
			bool isDark = IsDarkThemeEnabled();
			SetImmersiveDarkMode(hwnd, isDark);
			Resources["AppBorderBrush"] = isDark 
				? new SolidColorBrush(Color.FromRgb(50, 50, 50))    // Dark border color
				: new SolidColorBrush(Color.FromRgb(200, 200, 200)); // Light border color
		}

		private static void SetImmersiveDarkMode(IntPtr hwnd, bool enabled)
		{
			int useDark = enabled ? 1 : 0;
			DwmSetWindowAttribute(hwnd,
				DWMWA_USE_IMMERSIVE_DARK_MODE,
				ref useDark,
				sizeof(int));
		}
		
		public static bool IsDarkThemeEnabled()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(
				       @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false))
			{
				if (key is null) return false;  // assume light
				var v = key.GetValue("AppsUseLightTheme");
				return v is int intVal && intVal == 0;
			}
		}
	}
}
