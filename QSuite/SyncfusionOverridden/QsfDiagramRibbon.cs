using Syncfusion.UI.Xaml.DiagramRibbon;
using Syncfusion.Windows.Tools.Controls;

namespace Qenex.QSuite.SyncfusionOverridden;

public class QsfDiagramRibbon : SfDiagramRibbon
{
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        Tabs.Add(new RibbonTab(){Caption = "aaadEEEE"});
        
    }
}