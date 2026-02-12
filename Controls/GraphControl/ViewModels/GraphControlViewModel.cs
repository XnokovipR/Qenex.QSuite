using System.IO;
using System.Windows.Media.Imaging;
using OpenTK.Graphics.OpenGL;
using Qenex.QSuite.Common.WpfComm;
using Qenex.QSuite.Controls.Control;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.QVariables.Values;
using RtGraphControl.Models;
using ScottPlot;
using ScottPlot.WPF;
using Media = System.Windows.Media;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public class GraphControlViewModel : ControlBase
{
    private readonly string fontName = "Segoe UI";
    private Dictionary<string, ChartDataSeries> chartDataSeries;
    private DateTime baseTime;
    private DateTime lastUpdateTime;
    
    private readonly Dictionary<int, System.Drawing.Color> chartColors = new Dictionary<int, System.Drawing.Color>
    {
        { 1, System.Drawing.Color.Red },
        { 2, System.Drawing.Color.Blue },
        { 3, System.Drawing.Color.Green },
        { 4, System.Drawing.Color.Orange },
        { 5, System.Drawing.Color.Purple },
        { 6, System.Drawing.Color.Brown },
        { 7, System.Drawing.Color.Magenta },
        { 8, System.Drawing.Color.Cyan },
        { 9, System.Drawing.Color.Yellow },
        { 10, System.Drawing.Color.Gray },
        { 11, System.Drawing.Color.Pink },
        { 12, System.Drawing.Color.Lime },
        { 13, System.Drawing.Color.Teal },
        { 14, System.Drawing.Color.Navy },
        { 15, System.Drawing.Color.Maroon },
        { 16, System.Drawing.Color.Olive },
        { 17, System.Drawing.Color.Silver },
        { 18, System.Drawing.Color.Gold },
        { 19, System.Drawing.Color.Coral },
        { 20, System.Drawing.Color.Turquoise }
    };
    private int currentColorIndex = 1;
    
    
    #region Constructor

    public GraphControlViewModel()
    {
        Width = 300;
        Height = 200;

        PlotControl = new WpfPlot();
        
        //PlotControl.Plot.Title("Real-Time Graph");
        PlotControl.Plot.Axes.Title.Label.FontSize = 20;
        
        PlotControl.Plot.Axes.Bottom.Label.Text = "Time [s]";
        PlotControl.Plot.Axes.Bottom.Label.FontSize = 20;
        PlotControl.Plot.Axes.Bottom.TickLabelStyle.FontSize = 18;
        
        PlotControl.Plot.Axes.Left.Label.Text = "Value [-]";
        PlotControl.Plot.Axes.Left.Label.FontSize = 20;
        PlotControl.Plot.Axes.Left.TickLabelStyle.FontSize = 18;
        
        PlotControl.Plot.Axes.Top.Label.FontSize = 18;
        PlotControl.Plot.Axes.Top.TickLabelStyle.FontSize = 18;
        
        PlotControl.Plot.Axes.Right.Label.FontSize = 18;
        PlotControl.Plot.Axes.Right.TickLabelStyle.FontSize = 18;
        
        PlotControl.Plot.Axes.SetLimitsX(0, 5);
        PlotControl.Plot.Axes.SetLimitsY(0, 1);
        
        chartDataSeries = new Dictionary<string, ChartDataSeries>();
    }

    #endregion
    
    #region Properties

    public string ChartTitle
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;
    
    public WpfPlot PlotControl { get; }

    public int ChartTimeSpan
    {
        get;
        set
        {
            if (value < 1) value = 1;
            field = value; OnPropertyChanged();
        }
    } = 10;
    
    public int ChartBuffer
    { 
        get;
        set
        {
            if (value < 50) value = 50;
            field = value; OnPropertyChanged();
        } 
    } = 60;
    
    
    #endregion
    
    #region Derived properties

    public override string ControlName => "GraphControl";
    public override string Label => "Graph";
    public override BitmapImage Icon => ImageGetter.GetBitmapImage("Icons/GraphControl.png");
    public override string Description => "Graph Control for displaying data in a graphical format.";

    #endregion    
    
    #region Overrides of ControlBase

    public override void UpdateThemeSettingsControl(System.Windows.Media.Color backgroundColor, System.Windows.Media.Color foregroundColor, int fontSize)
    {
        base.UpdateThemeSettingsControl(backgroundColor, foregroundColor, fontSize);

        var plotFontSize = (int)Math.Round(1.8 * fontSize);
        var axesFontSize = (int)Math.Round(1.5 * fontSize);
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
        
        var bgColor = backgroundColor.ToScottPlotColor();
        var fgColor = foregroundColor.ToScottPlotColor();
            
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
        
        foreach (var axis in PlotControl.Plot.Axes.GetAxes())
        {
            axis.FrameLineStyle.Color = fgColor;
        }
        
        PlotControl.Refresh();
    }

    public override void BindVariable(IVariableBase variable)
    {
        var ev = Variables.FirstOrDefault(v => v.Equals(variable));
        if (ev != null) return;
        Variables.Add(variable);

        var c = chartColors[currentColorIndex++];
        var series = new ChartDataSeries(variable.Name, c);
        chartDataSeries.Add(variable.Name, series);
        
        PlotControl.Plot.Add.SignalXY(series.XVal, series.YVal, new Color(c));
    }

    public override Task UpdateVariableValueAsync(IVariableBase variable)
    {
        if (variable is ScalarVariable scalarVariable)
        {
            if (baseTime == DateTime.MinValue)
            {
                baseTime = DateTime.UtcNow;
                lastUpdateTime = baseTime;
            }
            
            chartDataSeries.TryGetValue(scalarVariable.Name, out var series);
            if (series == null) return Task.CompletedTask;

            var val = scalarVariable.Values switch
            {
                Values<int> vi => Convert.ToDouble(vi.Value),
                Values<double> vd => vd.Value,
                Values<float> vf => Convert.ToDouble(vf.Value),
                Values<bool> vf => Convert.ToDouble(vf.Value),
                Values<byte> vf => Convert.ToDouble(vf.Value),
                _ => 5

            };
            
            var timestamp = variable.Timestamp;
            
            series.XDateTimeVal.Add(timestamp);
            var xVal = (timestamp - baseTime).TotalSeconds;
            series.XVal.Add(xVal);
            series.YVal.Add(val);

            var top = PlotControl.Plot.Axes.GetLimits().Top;
            var bottom = PlotControl.Plot.Axes.GetLimits().Bottom;
            if (val > top)
            {
                PlotControl.Plot.Axes.SetLimitsY(bottom, val * 1.1);    
            }
            else if (val < bottom && val > 0)
            {
                PlotControl.Plot.Axes.SetLimitsY(val * 0.7, top);
            }
            else if (val < bottom && val < 0)
            {
                PlotControl.Plot.Axes.SetLimitsY(val * 1.1, top);
            }
            
            PlotControl.Plot.Axes.SetLimitsX(xVal - ChartTimeSpan - 1,xVal + 1);

            if ((timestamp - lastUpdateTime).TotalMilliseconds > ChartBuffer)
            {
                PlotControl.Refresh();
                lastUpdateTime = timestamp;
            }
            
        }
        return Task.CompletedTask;
    }

    #endregion
}

public class ChartDataSeries
{
    public string Name { get; set; }
    public System.Drawing.Color Color { get; set; }
    
    public List<DateTime> XDateTimeVal { get; set; }
    public List<double> XVal { get; set; }
    public List<double> YVal { get; set; }

    public ChartDataSeries(string name, System.Drawing.Color color)
    {
        Name = name;
        Color = color;
        XDateTimeVal = [];
        XVal = [];
        YVal = [];
    }
}