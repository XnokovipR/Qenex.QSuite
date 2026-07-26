using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Qenex.QInsight.Views;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.External;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using Qenex.QInsight.AppConfig;

namespace Qenex.QInsight
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			// Drivers resolve relative data paths (e.g. the default DataLogs directory) against
			// this root — the installation directory is not writable under Program Files.
			Qenex.QSuite.Drivers.Driver.DriverEnvironment.DataRootDirectory = AppDataPaths.Root;
			base.OnStartup(e);
		}
	}
}
