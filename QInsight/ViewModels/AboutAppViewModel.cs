using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qenex.QLibs.QUI;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public class AboutAppViewModel()
{
    private RadWindow parentWindow = null!;
    public string Version => "1.0.0";
    public string Copyright => $"© {DateTime.Now.Year}";
    
    public RelayCommand<object> CloseCommand => new RelayCommand<object>((o) =>
    {
        parentWindow?.Close();
    });
    
    public void SetParentWindow(RadWindow window)
    {
        parentWindow = window;
    }
}
