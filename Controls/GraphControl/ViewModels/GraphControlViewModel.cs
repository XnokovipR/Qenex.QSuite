using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using Qenex.QSuite.Controls.GraphControl.Helpers;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using RtGraphControl.Models;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public class GraphControlViewModel : ControlBase, IHasMousePosition
{
    #region Fields

    private DateTime baseTime;
    private DateTime lastUpdateTime;
    private int currentColorIndex = 1;
    private readonly Crosshair cross;
    private readonly Annotation annotation;

    #endregion
    
    #region Constructor

    public GraphControlViewModel()
    {
        MouseMoveCommand = new RelayCommand<MouseEventArgs>(DisplayCursorBasedOnMouseMove);
        RemoveChartVariableCommand = new RelayCommand<object>(RemoveChartVariable);
        ClearGraphCommand = new RelayCommand<object>(ClearGraph);
        
        Width = 300;
        Height = 200;

        PlotControl = new WpfPlot();
        cross = PlotControl.Plot.Add.Crosshair(0, 0);
        annotation = PlotControl.Plot.Add.Annotation("", Alignment.UpperLeft);
        annotation.IsVisible = false;
        cross.IsVisible = false;
        
        // Remove context menu
        PlotControl.Menu?.Clear();
        PlotControl.UserInputProcessor.UserActionResponses.RemoveAll(
            x => x is ScottPlot.Interactivity.UserActionResponses.SingleClickContextMenu);
        
        //PlotControl.Plot.Title("Real-Time Graph");
        PlotControl.Plot.Axes.Title.Label.FontSize = 20;
        
        PlotControl.Plot.Axes.Bottom.Label.Text = "Time [s]";
        PlotControl.Plot.Axes.Bottom.Label.FontSize = 20;
        PlotControl.Plot.Axes.Bottom.TickLabelStyle.FontSize = 18;

        PlotControl.Plot.Axes.Left.Label.IsVisible = false;
        PlotControl.Plot.Axes.Left.Label.Text = string.Empty;
        PlotControl.Plot.Axes.Left.Label.FontSize = 20;
        PlotControl.Plot.Axes.Left.TickLabelStyle.FontSize = 18;
        
        PlotControl.Plot.Axes.Top.Label.FontSize = 18;
        PlotControl.Plot.Axes.Top.TickLabelStyle.FontSize = 18;

        PlotControl.Plot.Axes.Right.Label.IsVisible = false;
        PlotControl.Plot.Axes.Right.Label.FontSize = 18;
        PlotControl.Plot.Axes.Right.TickLabelStyle.FontSize = 18;
        
        PlotControl.Plot.Axes.SetLimitsX(0, 10);
        PlotControl.Plot.Axes.SetLimitsY(-10, 10);
        
        ChartVariables = new ObservableCollection<ChartVariable>();
    }

    #endregion
    
    #region Properties

    public ChartVariable SelectedChartVariable
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public bool IsLegendHorizontal 
    {
        get;
        set
        {
            field = value; 
            OnPropertyChanged();
            PlotControl.Plot.Legend.Orientation = value ? ScottPlot.Orientation.Horizontal : ScottPlot.Orientation.Vertical;
            PlotControl.Refresh();
        } 
    }

    public Point MousePosition { get; set; }

    public bool IsCrossEnabled
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            cross.IsVisible = value;
            annotation.IsVisible = value;
            PlotControl.Refresh();
        }
    } = false;

    public RelayCommand<MouseEventArgs> MouseMoveCommand { get; set; }
    public RelayCommand<object> ClearGraphCommand { get; set; }
    public RelayCommand<object> RemoveChartVariableCommand { get; set; }
    
    
    public ObservableCollection<ChartVariable> ChartVariables { get; set; }

    public string ChartTitle { get; set { field = value; OnPropertyChanged(); } } = string.Empty;
    
    public WpfPlot PlotControl { get; }

    public int ChartTimeSpan { get; set { if (value < 1) value = 1; field = value; OnPropertyChanged(); } } = 10;
    
    public int ChartBuffer { get; set { if (value < 50) value = 50; field = value; OnPropertyChanged(); } } = 80;
    
    #endregion
    
    #region Derived properties

    public override string ControlName => "GraphControl";
    public override string Label => "Graph";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/GraphControl.png");
    public override string Description => "Graph Control for displaying data in a graphical format.";

    #endregion    
    
    #region Overrides of ControlBase
    
    public override void BindVariable(IVariableBase variable)
    {
        var ev = Variables.FirstOrDefault(v => v.Equals(variable));
        if (ev != null) return;
        Variables.Add(variable);
        
        var c = ChartHelper.ChartColors[currentColorIndex++];
        var chartVariable = new ChartVariable(variable);
        ChartVariables.Add(chartVariable);
        
        var signal = PlotControl.Plot.Add.SignalXY(chartVariable.XVal, chartVariable.YVal, ChartVariable.ToScottPlotColor(c));
        signal.LegendText = $"{variable.Label} ({variable.Id})";
        // signal.MarkerShape = MarkerShape.Asterisk;
        // signal.MarkerSize = 50; 
        chartVariable.ChartSignal = signal;
        chartVariable.ChartColor = c;
        chartVariable.Variable = variable;

        PlotControl.Refresh();

        //PlotControl.Plot.Remove(signal);
    }

    public override Task UpdateVariableValueAsync(IVariableBase variable)
    {
        // Only handle scalar variables contained in ChartVariables
        if (variable is not ScalarVariable scalarVariable) return Task.CompletedTask;
        var chartVariable = ChartVariables.FirstOrDefault(v => v.Variable.Name == scalarVariable.Name);
        if (chartVariable == null) return Task.CompletedTask;

        // Initialize baseTime and lastUpdateTime on the first update
        if (baseTime == DateTime.MinValue)
        {
            baseTime = DateTime.UtcNow;
            lastUpdateTime = baseTime;
        }

        var val = ConvertValueToDouble(scalarVariable);

        var timestamp = variable.Timestamp;
        chartVariable.XDateTimeVal.Add(timestamp);
        var xVal = (timestamp - baseTime).TotalSeconds;
        chartVariable.XVal.Add(xVal);
        chartVariable.YVal.Add(val);

        RecalculateAxisLimits(xVal, val);

        if ((timestamp - lastUpdateTime).TotalMilliseconds > ChartBuffer)
        {
            PlotControl.Refresh();
            lastUpdateTime = timestamp;
        }

        if (IsCrossEnabled)
        {
            ActualizeCrosshairAndAnnotation(MousePosition);
        }



        return Task.CompletedTask;
    }
    
    public override void UpdateThemeSettingsControl(System.Windows.Media.Color backgroundColor, System.Windows.Media.Color foregroundColor, int fontSize)
    {
        base.UpdateThemeSettingsControl(backgroundColor, foregroundColor, fontSize);

        var plotFontSize = (int)Math.Round(1.8 * fontSize);
        var axesFontSize = (int)Math.Round(1.5 * fontSize);
        var bgColor = backgroundColor.ToScottPlotColor();
        var fgColor = foregroundColor.ToScottPlotColor();
        
        // Crosshair
        cross.LineColor = fgColor;
        
        // Annotation
        annotation.LabelFontSize = plotFontSize;
        annotation.LabelFontColor = fgColor;
        annotation.LabelBackgroundColor = bgColor;
        
        PlotControl.Plot.Legend.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Title.Label.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Title.Label.Bold = false;
        PlotControl.Plot.Axes.Bottom.Label.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Bottom.Label.Bold = false;
        PlotControl.Plot.Axes.Top.Label.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Top.Label.Bold = false;
        PlotControl.Plot.Axes.Left.Label.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Left.Label.Bold = false;
        PlotControl.Plot.Axes.Right.Label.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Right.Label.Bold = false;
        PlotControl.Plot.Axes.Bottom.TickLabelStyle.FontSize = axesFontSize;
        PlotControl.Plot.Axes.Left.TickLabelStyle.FontSize = axesFontSize;
        PlotControl.Plot.Axes.Top.TickLabelStyle.FontSize = axesFontSize;
        PlotControl.Plot.Axes.Right.TickLabelStyle.FontSize = axesFontSize;
            
        PlotControl.Plot.FigureBackground.Color = bgColor;
        PlotControl.Plot.DataBackground.Color = bgColor;
        PlotControl.Plot.DataBorder.Color = fgColor;
        
        PlotControl.Plot.Axes.Title.Label.ForeColor = fgColor;
        PlotControl.Plot.Axes.Title.Label.BackgroundColor = bgColor;
        
        PlotControl.Plot.Axes.Bottom.Label.ForeColor = fgColor;
        PlotControl.Plot.Axes.Bottom.Label.BackgroundColor = bgColor;
        PlotControl.Plot.Axes.Bottom.TickLabelStyle.ForeColor = fgColor;
        PlotControl.Plot.Axes.Bottom.TickLabelStyle.BackgroundColor = bgColor;
        
        PlotControl.Plot.Axes.Left.Label.ForeColor = fgColor;
        PlotControl.Plot.Axes.Left.Label.BackgroundColor = bgColor;
        PlotControl.Plot.Axes.Left.TickLabelStyle.ForeColor = fgColor;
        PlotControl.Plot.Axes.Left.TickLabelStyle.BackgroundColor = bgColor;
        
        PlotControl.Plot.Axes.Top.Label.ForeColor = fgColor;
        PlotControl.Plot.Axes.Top.Label.BackgroundColor = bgColor;
        PlotControl.Plot.Axes.Top.TickLabelStyle.ForeColor = fgColor;
        PlotControl.Plot.Axes.Top.TickLabelStyle.BackgroundColor = bgColor;      
        
        PlotControl.Plot.Axes.Right.Label.ForeColor = fgColor;
        PlotControl.Plot.Axes.Right.Label.BackgroundColor = bgColor;
        PlotControl.Plot.Axes.Right.TickLabelStyle.ForeColor = fgColor;
        PlotControl.Plot.Axes.Right.TickLabelStyle.BackgroundColor = bgColor;

        // Legend
        PlotControl.Plot.Legend.BackgroundColor = bgColor;
        PlotControl.Plot.Legend.FontColor = fgColor;
        PlotControl.Plot.Legend.Alignment = Alignment.LowerLeft;
        
        // Grid
        PlotControl.Plot.Grid.LineColor = new Color((fgColor.R + bgColor.R)/2, (fgColor.G + bgColor.G)/2, (fgColor.B + bgColor.B)/2, 0.25f);
        
        
        foreach (var axis in PlotControl.Plot.Axes.GetAxes())
        {
            axis.FrameLineStyle.Color = fgColor;
        }
        
        PlotControl.Plot.ShowLegend();
        PlotControl.Refresh();
    }

    #endregion

    #region Private Methods

    private void RecalculateAxisLimits(double xVal, double yVal)
    {
        var top = PlotControl.Plot.Axes.GetLimits().Top;
        var bottom = PlotControl.Plot.Axes.GetLimits().Bottom;
        if (yVal > top)
        {
            PlotControl.Plot.Axes.SetLimitsY(bottom, yVal * 1.1);    
        }
        else if (yVal < bottom && yVal > 0)
        {
            PlotControl.Plot.Axes.SetLimitsY(yVal * 0.7, top);
        }
        else if (yVal < bottom && yVal < 0)
        {
            PlotControl.Plot.Axes.SetLimitsY(yVal * 1.1, top);
        }
        
        PlotControl.Plot.Axes.SetLimitsX(xVal - ChartTimeSpan - 1,xVal + 1);
    }

    private double ConvertValueToDouble(ScalarVariable scalarVariable)
    {
        return scalarVariable.Values switch
        {
            Values<int> vi => Convert.ToDouble(vi.Value),
            Values<double> vd => vd.Value,
            Values<float> vf => Convert.ToDouble(vf.Value),
            Values<bool> vf => Convert.ToDouble(vf.Value),
            Values<byte> vf => Convert.ToDouble(vf.Value),
            _ => throw new InvalidCastException("Unsupported variable type")
        };
    }
    
    private void DisplayCursorBasedOnMouseMove(MouseEventArgs e)
    {
        if (!IsCrossEnabled) return;
        
        var p = e.GetPosition(PlotControl);
        ActualizeCrosshairAndAnnotation(p);
        PlotControl.Refresh();
    }
    
    private void ActualizeCrosshairAndAnnotation(Point p)
    {
        Pixel mousePixel = new(p.X * PlotControl.DisplayScale, p.Y * PlotControl.DisplayScale);
        Coordinates coordinates = PlotControl.Plot.GetCoordinates(mousePixel);
        cross.Position = coordinates;
        annotation.Text = $"{Math.Round(cross.Position.X, 3)}; {Math.Round(cross.Position.Y, 3)}";
    }
    
    private void RemoveChartVariable(object parameter)
    {
        if (SelectedChartVariable != null && SelectedChartVariable.ChartSignal != null)
        {
            var index = ChartVariables.IndexOf(SelectedChartVariable);
            var cVar = SelectedChartVariable.Variable;
            PlotControl.Plot.Remove(SelectedChartVariable.ChartSignal);
            ChartVariables.Remove(SelectedChartVariable);
            Variables.Remove(cVar);
            if (ChartVariables.Count > 0)
            {
                SelectedChartVariable = ChartVariables[Math.Min(index, ChartVariables.Count - 1)];
            }
            PlotControl.Refresh();
        }
    }
    
    private void ClearGraph(object parameter)
    {
        baseTime = DateTime.MinValue;
        lastUpdateTime = baseTime;

        foreach (var chartVariable in ChartVariables)
        {
            chartVariable.XDateTimeVal.Clear();
            chartVariable.XVal.Clear();
            chartVariable.YVal.Clear();
        }
        
        PlotControl.Refresh();
    }

    #endregion
}