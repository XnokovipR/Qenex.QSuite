using System.Windows.Media.Imaging;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.Control;
using System.Drawing;
using System.Windows.Media;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public class GraphControlViewModel : ControlBase
{
	private string text;

	public GraphControlViewModel()
    {
        var rnd = new Random();
        Text = $"Graph Control {rnd.Next(1, 100)}";
        Width = 300;
        Height = 200;
        BackgroundColor = Colors.Aqua;
	}

	public string Text { get => text; set { text = value; OnPropertyChanged(); } }

	public override string ControlName => "GraphControl";
    public override string Label => "Graph";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/GraphControl.png");
    public override string Description => "Graph Control for displaying data in a graphical format.";
}