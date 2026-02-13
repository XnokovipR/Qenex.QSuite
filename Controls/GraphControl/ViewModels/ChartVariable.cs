using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.QVariables;
using ScottPlot;
using ScottPlot.Plottables;
using Color = System.Windows.Media.Color;


namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public class ChartVariable : PropertyChangedBase
{
    #region Constructors

    public ChartVariable(IVariableBase variable)
    {
        XDateTimeVal = [];
        XVal = [];
        YVal = [];        
    }

    #endregion

    #region Properties

    public IVariableBase Variable { get; set; } = null!;
    
    public Color ChartColor { get; set { field = value; OnPropertyChanged(); ChartSignal?.Color = ToScottPlotColor(value); } }
    
    public SignalXY? ChartSignal { get; set { field = value; OnPropertyChanged(); } } = null!;
    
    public List<DateTime> XDateTimeVal { get; set; }
    public List<double> XVal { get; set; }
    public List<double> YVal { get; set; } 

    #endregion


    #region Static Methods

    public static ScottPlot.Color ToScottPlotColor(Color c) => new ScottPlot.Color(c.R, c.G, c.B, c.A);

    #endregion
}