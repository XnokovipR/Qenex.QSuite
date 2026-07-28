using System.Windows.Navigation;
using Qenex.QInsight.Helpers;

namespace Qenex.QInsight.Views;

public partial class AboutAppWindow
{
    public AboutAppWindow()
    {
        InitializeComponent();
    }

    // Thin forwarding only (no application logic in code-behind).
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        WebBrowserLauncher.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}