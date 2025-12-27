using System.Windows.Media.Imaging;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Controls.Control;
using System.Drawing;
using System.Windows.Media;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public sealed class GraphControlViewModel : ControlBase
{
	public GraphControlViewModel()
    {
        var rnd = new Random();
        Text = $"Graph Control {rnd.Next(1, 100)}";
        Width = 300;
        Height = 200;
        BackgroundColor = Colors.Aqua;
	}

	public string Text { get; set { field = value; OnPropertyChanged(); } }

	public override string ControlName => "GraphControl";
    public override string Label => "Graph";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/GraphControl.png");
    public override string Description => "Graph Control for displaying data in a graphical format.";
    public override Task UpdateVariableValueAsync(IVariableBase variable)
    {
	    return Task.CompletedTask;
    }

    public override void BindVariable(IVariableBase protVariable)
    {
	    
    }
}