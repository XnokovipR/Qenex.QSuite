using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Qenex.QSuite.Controls.SingleSignalControl.ViewModels;

public class SingleSignalControlViewModel : ControlBase
{
	private Dictionary<int, Color> colors = new Dictionary<int, Color>()
	{
		[0] = Colors.Red,
		[1] = Colors.Blue,
		[2] = Colors.Yellow,
		[3] = Colors.Orange,
		[4] = Colors.Green,
		[5] = Colors.Purple,
		[6] = Colors.Brown,
		[7] = Colors.Cyan,
		[8] = Colors.Magenta,
		[9] = Colors.Lime
	};
	
    public SingleSignalControlViewModel()
    {
		Width = 300;
		Height = 200;
		BackgroundColor = colors[new Random().Next(colors.Count)];
	}

    public string Text { get; set; }

    public override string ControlName => "SignalControl";
    public override string Label => "Single-Signal";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/SingleSignalControl.png");
    public override string Description => "Graph Control for displaying data in a graphical format.";
}