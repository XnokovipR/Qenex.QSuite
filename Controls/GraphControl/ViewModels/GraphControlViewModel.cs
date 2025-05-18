using System.Windows.Media.Imaging;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public class GraphControlViewModel : ControlBase
{
    public override string ControlName => "GraphControl";
    public override string Label => "Graph";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/GraphControl.png");
    public override string Description => "Graph Control for displaying data in a graphical format.";
}