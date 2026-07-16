using RtGraphControl.Models;
using ScottPlot;
using ScottPlot.AxisPanels;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public sealed class VerticalAxis : YAxisBase
{
    public Action? RefreshAction;
    public VerticalAxis(Edge edge = Edge.Right)
    {
        Edge = edge;
        
        TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
        LabelRotation = -90;
    }
    
    public override Edge Edge { get; }
    
    public string Name { get; set; } = string.Empty;
    
    public bool IsAutoScale { get; set; } = true;

    public double Minimum 
    { 
        get;
        set 
        {
            field = value;
            Min = value;
            RefreshAction?.Invoke();
        } 
    } = -10.0;
    
    public double Maximum
    {
        get;
        set
        {
            field = value;
            Max = value;
            RefreshAction?.Invoke();
        }
    } = 10.0;

    public ScottPlot.Color ThemeForeColor { get; set; }

    public System.Windows.Media.Color? AxisColor
    {
        get;
        set
        {
            field = value;
            ApplyColor();
            RefreshAction?.Invoke();
        }
    }

    public void ApplyColor()
    {
        var color = AxisColor?.ToScottPlotColor() ?? ThemeForeColor;

        FrameLineStyle.Color = color;
        MajorTickStyle.Color = color;
        MinorTickStyle.Color = color;
        TickLabelStyle.ForeColor = color;
        TickLabelStyle.PointColor = color;
        LabelFontColor = color;
        LabelBorderColor = color;
    }
}