using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Qenex.QSuite.Controls.SingleSignalControl.ViewModels;

public class SingleSignalControlViewModel : ControlBase
{
    public SingleSignalControlViewModel()
    {
		Width = 300;
		Height = 200;
		BackgroundColor = Colors.LightGray;
	}

    public string Text { get; set; }

    public override string ControlName => "SignalControl";
    public override string Label => "Single-Signal";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SingleSignalControl.png");
    public override string Description => "Graph Control for displaying data in a graphical format.";
}