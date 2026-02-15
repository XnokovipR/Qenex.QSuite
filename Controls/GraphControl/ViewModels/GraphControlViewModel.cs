using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
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
using ScottPlot.AxisPanels;
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
    private Color backgroundColor;
    private Color foregroundColor;
    private int axesFontSize;
    private int plotFontSize;

    #endregion
    
    #region Constructor

    public GraphControlViewModel()
    {
        ChartVariables = [];
        VerticalAxes = [];
        
        MouseMoveCommand = new RelayCommand<MouseEventArgs>(DisplayCursorBasedOnMouseMove);
        RemoveChartVariableCommand = new RelayCommand<object>(RemoveChartVariable);
        ClearGraphCommand = new RelayCommand<object>(ClearGraph);
        AddAxisCommand = new RelayCommand<object>(AddAxis);
        RemoveAxisCommand = new RelayCommand<object>(RemoveAxis);
        ZoomToFitCommand = new RelayCommand<object>((i) => PlotControl?.Plot.Axes.AutoScale()); 
        
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
        
        PlotControl.Plot.Axes.Remove(Edge.Left);
        //PlotControl.Plot.Axes.Remove(Edge.Right);
    }

    #endregion
    
    #region Properties
    
    public ObservableCollection<IYAxis> VerticalAxes { get; set { field = value; OnPropertyChanged(); } }
    public IYAxis SelectedVerticalAxis { get; set { field = value; OnPropertyChanged(); } }

    public ChartVariable SelectedChartVariable { get; set { field = value; OnPropertyChanged(); } }

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
    public RelayCommand<object> AddAxisCommand { get; set; }
    public RelayCommand<object> RemoveAxisCommand { get; set; }
    
    public RelayCommand<object> ZoomToFitCommand { get; set; }
    
    
    
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
        var chartVariable = new ChartVariable();
        ChartVariables.Add(chartVariable);
        
        var signal = PlotControl.Plot.Add.SignalXY(chartVariable.XVal, chartVariable.YVal, ChartVariable.ToScottPlotColor(c));
        signal.LegendText = $"{variable.Label} ({variable.Id})";
        // signal.MarkerShape = MarkerShape.Asterisk;
        // signal.MarkerSize = 50; 
        chartVariable.ChartSignal = signal;
        chartVariable.ChartColor = c;
        chartVariable.Variable = variable;
        chartVariable.ChangeAxisAction += axisIndex =>
        {
            var retIndex = axisIndex;
            if (axisIndex < 0 || axisIndex >= VerticalAxes.Count)
            {
                retIndex = 0;
                signal.Axes.YAxis = VerticalAxes[retIndex];    
            }
            else
            {
                signal.Axes.YAxis = VerticalAxes[axisIndex];    
            }
            
            PlotControl.Refresh();
            return retIndex;
        };

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

        if (((VerticalAxis)VerticalAxes[chartVariable.AxisIndex]).IsAutoScale)
        {
            RecalculateVerticalAxisLimits(xVal, val, chartVariable.AxisIndex);
        }

        PlotControl.Plot.Axes.SetLimitsX(xVal - ChartTimeSpan - 1,xVal + 1);

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
    
    public override void UpdateThemeSettingsControl(System.Windows.Media.Color bgColor, System.Windows.Media.Color fgColor, int fontSize)
    {
        base.UpdateThemeSettingsControl(bgColor, fgColor, fontSize);

        plotFontSize = (int)Math.Round(1.8 * fontSize);
        axesFontSize = (int)Math.Round(1.5 * fontSize);
        backgroundColor = bgColor.ToScottPlotColor();
        foregroundColor = fgColor.ToScottPlotColor();
        
        // Crosshair
        cross.LineColor = foregroundColor;
        
        // Legend
        PlotControl.Plot.Legend.FontSize = axesFontSize;
        PlotControl.Plot.Legend.BackgroundColor = backgroundColor;
        PlotControl.Plot.Legend.FontColor = foregroundColor;
        PlotControl.Plot.Legend.Alignment = Alignment.LowerLeft;
        
        // Annotation
        annotation.LabelFontSize = plotFontSize;
        annotation.LabelFontColor = foregroundColor;
        annotation.LabelBackgroundColor = backgroundColor;
        
        // Background
        PlotControl.Plot.FigureBackground.Color = backgroundColor;
        PlotControl.Plot.DataBackground.Color = backgroundColor;
        PlotControl.Plot.DataBorder.Color = foregroundColor;
        
        // Set Title Axis <<
        PlotControl.Plot.Axes.Title.Label.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Title.Label.Bold = false;
        PlotControl.Plot.Axes.Title.Label.ForeColor = foregroundColor;
        PlotControl.Plot.Axes.Title.Label.BackgroundColor = backgroundColor;
        PlotControl.Plot.Axes.Title.IsVisible = false;
        
        
        // Set horizontal axes
        PlotControl.Plot.Axes.SetLimitsX(0, 10);
        PlotControl.Plot.Axes.Bottom.Label.Text = "Time [s]";
        PlotControl.Plot.Axes.Bottom.Label.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Bottom.Label.Bold = false;   
        PlotControl.Plot.Axes.Bottom.TickLabelStyle.FontSize = axesFontSize;
        PlotControl.Plot.Axes.Bottom.Label.ForeColor = foregroundColor;
        PlotControl.Plot.Axes.Bottom.Label.BackgroundColor = backgroundColor;
        PlotControl.Plot.Axes.Bottom.TickLabelStyle.ForeColor = foregroundColor;
        PlotControl.Plot.Axes.Bottom.TickLabelStyle.BackgroundColor = backgroundColor;

        PlotControl.Plot.Axes.Top.Label.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Top.Label.Bold = false;
        PlotControl.Plot.Axes.Top.TickLabelStyle.FontSize = axesFontSize;    
        PlotControl.Plot.Axes.Top.Label.ForeColor = foregroundColor;
        PlotControl.Plot.Axes.Top.Label.BackgroundColor = backgroundColor;
        PlotControl.Plot.Axes.Top.TickLabelStyle.ForeColor = foregroundColor;
        PlotControl.Plot.Axes.Top.TickLabelStyle.BackgroundColor = backgroundColor; 
        
        // Set right empty axis as default
        PlotControl.Plot.Axes.Right.Label.IsVisible = false;
        PlotControl.Plot.Axes.Right.Label.FontSize = plotFontSize;
        PlotControl.Plot.Axes.Right.Label.Bold = false;
        PlotControl.Plot.Axes.Right.TickLabelStyle.FontSize = axesFontSize;    
        PlotControl.Plot.Axes.Right.Label.ForeColor = foregroundColor;
        PlotControl.Plot.Axes.Right.Label.BackgroundColor = backgroundColor;
        PlotControl.Plot.Axes.Right.TickLabelStyle.ForeColor = foregroundColor;
        PlotControl.Plot.Axes.Right.TickLabelStyle.BackgroundColor = backgroundColor; 
        
        // Add horizontal axes
        AddAxis(Edge.Left, 0);
        PlotControl.Plot.Axes.SetLimitsY(-10, 10);
        
        // Grid
        var gridColor = new Color((foregroundColor.R + backgroundColor.R)/2, (foregroundColor.G + backgroundColor.G)/2, (foregroundColor.B + backgroundColor.B)/2, 0.2f);
        PlotControl.Plot.Grid.LineColor = gridColor;
        PlotControl.Plot.Grid.YAxis = VerticalAxes[0];
        PlotControl.Plot.Grid.YAxisStyle.MajorLineStyle.IsVisible = true;
        PlotControl.Plot.Grid.YAxisStyle.MajorLineStyle.Color = gridColor;
     

        foreach (var axis in PlotControl.Plot.Axes.GetAxes())
        {
            axis.FrameLineStyle.Color = foregroundColor;
        }
        
        PlotControl.Plot.ShowLegend();
        PlotControl.Refresh();
    }

    #endregion

    #region Private Methods

    private void RecalculateVerticalAxisLimits(double xVal, double yVal, int axisIndex)
    {
        var yAxis = PlotControl.Plot.Axes.GetAxes().Where(x => x is VerticalAxis).Cast<VerticalAxis>().FirstOrDefault(x => x.Name.Contains($"Y->{axisIndex}"));
        if (yAxis is null) return;

        var top = yAxis.Max;
        var bottom = yAxis.Min;
        
        //var top = PlotControl.Plot.Axes.GetLimits().Top;
        //var bottom = PlotControl.Plot.Axes.GetLimits().Bottom;
        if (yVal > top)
        {
            yAxis.Max = yVal * 1.1;
            //PlotControl.Plot.Axes.SetLimitsY(bottom, yVal * 1.1);    
        }
        else if (yVal < bottom && yVal > 0)
        {
            yAxis.Min = yVal * 0.7;
            //PlotControl.Plot.Axes.SetLimitsY(yVal * 0.7, top);
        }
        else if (yVal < bottom && yVal < 0)
        {
            yAxis.Min = yVal * 1.1;
            //PlotControl.Plot.Axes.SetLimitsY(yVal * 1.1, top);
        }
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
    
    private void AddAxis(object parameter)
    {
        AddAxis(Edge.Right, VerticalAxes.Count);
        
    }

    private void AddAxis(Edge edge, int index)
    {
        var newAxis = new VerticalAxis(edge)
        {
            Name = $"Y->{index}",
            IsVisible = true,
            LabelBackgroundColor = backgroundColor,
            LabelFontColor = foregroundColor,
            LabelFontSize = plotFontSize,
            LabelBorderColor = foregroundColor,
            TickLabelStyle = new LabelStyle()
            {
                ForeColor = foregroundColor,
                BackgroundColor = backgroundColor,
                FontSize = axesFontSize,
                PointColor = foregroundColor,
            },

            MajorTickStyle = new TickMarkStyle()
            {
                Color = foregroundColor,
            },
            MinorTickStyle = new TickMarkStyle()
            {
                Color = foregroundColor,
            },
            FrameLineStyle =
            {
                Color = foregroundColor
            }
        };
        
        newAxis.RefreshAction = () =>
        {
            PlotControl.Refresh();
        };

        PlotControl.Plot.Axes.AddYAxis(newAxis);
        PlotControl.Plot.Axes.SetLimitsY(bottom: -10, top: 10, yAxis: newAxis);
        
        VerticalAxes.Add(newAxis);
        PlotControl.Refresh();        
    }
    
    
    private void RemoveAxis(object parameter)
    {
        if (VerticalAxes.Count > 1 && SelectedVerticalAxis != null)
        {
            var index = VerticalAxes.IndexOf(SelectedVerticalAxis);
            if (index == 0) return;
            
            PlotControl.Plot.Axes.Remove(SelectedVerticalAxis);
            VerticalAxes.Remove(SelectedVerticalAxis);
            if (VerticalAxes.Count > 0)
            {
                SelectedVerticalAxis = VerticalAxes[Math.Min(index, VerticalAxes.Count - 1)];
            }
            PlotControl.Refresh();
        }
    }

    #endregion
}