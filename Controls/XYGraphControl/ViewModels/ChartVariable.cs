using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.QVariables;
using ScottPlot;
using ScottPlot.Plottables;
using Color = System.Windows.Media.Color;


namespace Qenex.QSuite.Controls.XYGraphControl.ViewModels;

public class ChartVariable : PropertyChangedBase
{
    #region Constructors

    public ChartVariable()
    {
        XDateTimeVal = [];
        XVal = [];
        YVal = [];   
        LineWidth = 1.0f;
        IsVisible = true;
    }

    #endregion

    #region Properties
    
    public Func<int, int>? ChangeAxisAction { get; set; }

    // Invoked when this series is picked as the X-source (the radio behaviour — clearing the
    // others, hiding this series, relabelling the X axis — lives in the ViewModel).
    public Action<ChartVariable>? XAxisSelectedAction { get; set; }

    public IVariableBase Variable { get; set { field = value; OnPropertyChanged(); } } = null!;

    // True for the one series that provides the X coordinate; all others are plotted against it.
    public bool IsXAxis
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged();
            if (value)
            {
                XAxisSelectedAction?.Invoke(this);
            }
        }
    }
    
    public Color ChartColor { get; set { field = value; OnPropertyChanged(); ChartSignal?.Color = ToScottPlotColor(value); } }
    
    public float LineWidth { get; set { field = value; OnPropertyChanged(); ChartSignal?.LineWidth = value; } }

    public ChartLineStyle LineStyle
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            if (ChartSignal != null)
            {
                ChartSignal.LineStyle.Pattern = value.ToLinePattern();
            }
        }
    }
    
    public bool IsVisible { get; set { field = value; OnPropertyChanged(); ChartSignal?.IsVisible = value; } }
    
    // XY graph: the series is a Scatter (X and Y both arbitrary — the curve may loop back, e.g.
    // Lissajous/hysteresis), not a SignalXY (which requires X ascending, i.e. time).
    public Scatter? ChartSignal { get; set { field = value; OnPropertyChanged(); } } = null!;
    
    public int AxisIndex
    {
        get;
        set
        {
            field = ChangeAxisAction?.Invoke(value) ?? value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AxisNumber));
        }
    }

    // 1-based cislo osy pro UI (interni AxisIndex zustava 0-based kvuli persistenci)
    public int AxisNumber
    {
        get => AxisIndex + 1;
        set => AxisIndex = value - 1;
    }
    
    public List<DateTime> XDateTimeVal { get; set; }
    public List<double> XVal { get; set; }
    public List<double> YVal { get; set; } 

    public void RefreshVariableLabel()
    {
        if (Variable != null && ChartSignal != null)
        {
            ChartSignal.LegendText = $"{Variable.Label} ({Variable.Id})";
        }

        OnPropertyChanged(nameof(Variable));
    }

    #endregion

    #region Private Methods

    

    #endregion


    #region Static Methods

    public static ScottPlot.Color ToScottPlotColor(Color c) => new ScottPlot.Color(c.R, c.G, c.B, c.A);

    #endregion
}
