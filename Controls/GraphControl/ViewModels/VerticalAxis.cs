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
    


}