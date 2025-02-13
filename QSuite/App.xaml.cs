using System.Configuration;
using System.Data;
using System.Windows;

namespace Qenex.QSuite;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF1cWWhIfEx1RHxQdld5ZFRHallYTnNWUj0eQnxTdEBjW39XcXZQQmNYVURxWA==");
    }
}