using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Qenex.QInsight.Views;
using Telerik.Windows.Controls;

namespace Qenex.QInsight
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			
			Windows11ThemeSizeHelper.Helper.IsInCompactMode = true;
			Windows11Palette.Palette.FontSize = 12;

			Windows11Palette.LoadPreset(ShellWindow.IsDarkThemeEnabled() ? 
				Windows11Palette.ColorVariation.Dark : 
				Windows11Palette.ColorVariation.Light);
		}
	}
}
