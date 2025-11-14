using System.Windows.Media.Imaging;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;

namespace Qenex.QSuite.Controls.SingleSignalControl.ViewModels;

public class SingleSignalControlViewModel : ControlBase
{
    public SingleSignalControlViewModel()
    {
        var rnd = new Random();
        Text = $"Graph Control {rnd.Next(1, 100)}";
        Width = 100;
        Height = 70;
    }

    public string Text { get; set; }

    public override string ControlName => "SignalControl";
    public override string Label => "Single-Signal";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/GraphControl.png");
    public override string Description => "Graph Control for displaying data in a graphical format.";
}