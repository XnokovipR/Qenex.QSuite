using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Telerik.Windows.Controls;


namespace Qenex.QInsight.Views;

public partial class AboutAppWindow
{
    public AboutAppWindow()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }
}