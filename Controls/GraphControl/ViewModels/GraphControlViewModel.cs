using System.Collections.ObjectModel;
using System.Windows;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
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
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.FileDialogs;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

[DataContract]
public class GraphControlViewModel : ControlBase, IHasMousePosition
{
    #region Const

    private const double PlotFontSizeMultiplier = 0.9;

    #endregion
    
    #region Fields

    private DateTime baseTime;
    private DateTime lastUpdateTime;
    private int currentColorIndex;
    private Crosshair cross = null!;
    private Annotation annotation = null!;
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
        ExportImageCommand = new RelayCommand<object>(ExportImage);
        ExportCsvCommand = new RelayCommand<object>(ExportCsv);
        ZoomToFitCommand = new RelayCommand<object>((i) => PlotControl?.Plot.Axes.AutoScale()); 
        
        Width = 300;
        Height = 200;

        InitializeRuntimeState();
        //PlotControl.Plot.Axes.Remove(Edge.Right);
    }

    #endregion
    
    #region Properties
    
    [IgnoreDataMember]
    public ObservableCollection<IYAxis> VerticalAxes { get; set { field = value; OnPropertyChanged(); } }
    [IgnoreDataMember]
    public IYAxis SelectedVerticalAxis { get; set { field = value; OnPropertyChanged(); } }

    [IgnoreDataMember]
    public ChartVariable SelectedChartVariable { get; set { field = value; OnPropertyChanged(); } }

    [DataMember]
    public bool IsLegendHorizontal 
    {
        get;
        set
        {
            field = value; 
            OnPropertyChanged();
            if (PlotControl != null)
            {
                PlotControl.Plot.Legend.Orientation = value ? ScottPlot.Orientation.Horizontal : ScottPlot.Orientation.Vertical;
                PlotControl.Refresh();
            }
        } 
    }

    [IgnoreDataMember]
    public Point MousePosition { get; set; }

    [DataMember]
    public bool IsCrossEnabled
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
            if (cross != null)
            {
                cross.IsVisible = value;
            }

            if (annotation != null)
            {
                annotation.IsVisible = false;
            }

            if (!value && ChartVariables != null)
            {
                ResetLegendTexts();
            }

            PlotControl?.Refresh();
        }
    } = false;

    [IgnoreDataMember]
    public RelayCommand<MouseEventArgs> MouseMoveCommand { get; set; }
    [IgnoreDataMember]
    public RelayCommand<object> ClearGraphCommand { get; set; }
    [IgnoreDataMember]
    public RelayCommand<object> RemoveChartVariableCommand { get; set; }
    [IgnoreDataMember]
    public RelayCommand<object> AddAxisCommand { get; set; }
    [IgnoreDataMember]
    public RelayCommand<object> RemoveAxisCommand { get; set; }
    [IgnoreDataMember]
    public RelayCommand<object> ExportImageCommand { get; set; }
    [IgnoreDataMember]
    public RelayCommand<object> ExportCsvCommand { get; set; }
    
    [IgnoreDataMember]
    public RelayCommand<object> ZoomToFitCommand { get; set; }
    
    
    
    [IgnoreDataMember]
    public ObservableCollection<ChartVariable> ChartVariables { get; set; }

    [DataMember]
    public List<ChartVariableBinding> ChartVariableBindings { get; set; } = [];

    [DataMember]
    public List<VerticalAxisBinding> VerticalAxisBindings { get; set; } = [];

    [DataMember]
    public string ChartTitle { get; set { field = value; OnPropertyChanged(); } } = string.Empty;
    
    [IgnoreDataMember]
    public WpfPlot PlotControl { get; private set; } = null!;

    [IgnoreDataMember]
    public Action<DialogWindowBase>? ConfigureSaveFileDialog { get; set; }

    [IgnoreDataMember]
    public Func<string?>? SaveDialogInitialDirectoryProvider { get; set; }

    [IgnoreDataMember]
    public Action<string>? SaveDialogDirectoryChanged { get; set; }

    [DataMember]
    public int ChartTimeSpan { get; set { if (value < 1) value = 1; field = value; OnPropertyChanged(); } } = 10;
    
    [DataMember]
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

        RememberVariableBinding(variable);
        Variables.Add(variable);

        var savedBinding = GetSavedChartVariableBinding(variable);
        var c = GetSavedChartColor(savedBinding) ?? GetNextChartColor();
        RestoreVerticalAxes();
        var chartVariable = new ChartVariable();
        ChartVariables.Add(chartVariable);
        
        var signal = PlotControl.Plot.Add.SignalXY(chartVariable.XVal, chartVariable.YVal, ChartVariable.ToScottPlotColor(c));
        signal.LegendText = GetVariableLegendText(variable);
        // signal.MarkerShape = MarkerShape.Asterisk;
        // signal.MarkerSize = 50; 
        chartVariable.ChartSignal = signal;
        chartVariable.ChartColor = c;
        chartVariable.Variable = variable;
        chartVariable.ChangeAxisAction += axisIndex =>
        {
            var retIndex = GetValidVerticalAxisIndex(axisIndex);
            if (retIndex >= 0)
            {
                signal.Axes.YAxis = VerticalAxes[retIndex];
            }

            PlotControl.Refresh();
            return retIndex;
        };

        PlotControl.Refresh();
        chartVariable.LineWidth = GetSavedLineWidth(savedBinding);
        chartVariable.AxisIndex = savedBinding?.AxisIndex ?? 0;
        RememberChartVariableBinding(chartVariable);

        //PlotControl.Plot.Remove(signal);
    }

    public override Task UpdateVariableValueAsync(IVariableBase variable)
    {
        // Only handle scalar variables contained in ChartVariables
        if (variable is not ScalarVariable scalarVariable) return Task.CompletedTask;
        var chartVariable = ChartVariables.FirstOrDefault(v => v.Variable.Name == scalarVariable.Name);
        if (chartVariable == null) return Task.CompletedTask;

        var timestamp = variable.Timestamp == default
            ? DateTime.UtcNow
            : variable.Timestamp;

        // Initialize from the incoming data timestamp. Replay data can be historical,
        // so using DateTime.UtcNow here prevents refreshes from being triggered.
        if (baseTime == DateTime.MinValue)
        {
            baseTime = timestamp;
            lastUpdateTime = DateTime.MinValue;
        }

        var val = ConvertValueToDouble(scalarVariable);
        var xVal = (timestamp - baseTime).TotalSeconds;

        if (chartVariable.XDateTimeVal.Count > 0)
        {
            var lastChartVariableTimestamp = chartVariable.XDateTimeVal[^1];
            if (timestamp < lastChartVariableTimestamp)
            {
                ClearChartData(timestamp);
                xVal = 0;
            }
            else if (timestamp == lastChartVariableTimestamp)
            {
                chartVariable.YVal[^1] = val;
                RefreshPlotForValue(timestamp, xVal, val, chartVariable);
                return Task.CompletedTask;
            }
        }

        chartVariable.XDateTimeVal.Add(timestamp);
        chartVariable.XVal.Add(xVal);
        chartVariable.YVal.Add(val);

        RefreshPlotForValue(timestamp, xVal, val, chartVariable);

        return Task.CompletedTask;
    }

    public override void RefreshVariableBinding(IVariableBase variable)
    {
        base.RefreshVariableBinding(variable);

        foreach (var chartVariable in ChartVariables.Where(v =>
	                 v.Variable != null
	                 && (ReferenceEquals(v.Variable, variable)
	                     || ControlBase.IsVariableReferenceMatch(ControlBase.GetVariableReference(v.Variable), variable))))
        {
            chartVariable.Variable = variable;
            chartVariable.RefreshVariableLabel();
        }

        PlotControl?.Refresh();
    }

    private void ClearChartData(DateTime newBaseTime)
    {
        foreach (var chartVariable in ChartVariables)
        {
            chartVariable.XDateTimeVal.Clear();
            chartVariable.XVal.Clear();
            chartVariable.YVal.Clear();
        }

        baseTime = newBaseTime;
        lastUpdateTime = DateTime.MinValue;
    }

    private void RefreshPlotForValue(DateTime timestamp, double xVal, double val, ChartVariable chartVariable)
    {
        var axisIndex = GetValidVerticalAxisIndex(chartVariable.AxisIndex);
        if (axisIndex < 0)
        {
            return;
        }

        if (axisIndex != chartVariable.AxisIndex)
        {
            chartVariable.AxisIndex = axisIndex;
        }

        if (((VerticalAxis)VerticalAxes[axisIndex]).IsAutoScale)
        {
            RecalculateVerticalAxisLimits(xVal, val, axisIndex);
        }

        PlotControl.Plot.Axes.SetLimitsX(xVal - ChartTimeSpan - 1,xVal + 1);

        if (lastUpdateTime == DateTime.MinValue
            || timestamp < lastUpdateTime
            || (timestamp - lastUpdateTime).TotalMilliseconds > ChartBuffer)
        {
            PlotControl.Refresh();
            lastUpdateTime = timestamp;
        }

        if (IsCrossEnabled)
        {
            ActualizeCrosshairAndAnnotation(MousePosition);
        }
    }
    
    public override void UpdateThemeSettingsControl(System.Windows.Media.Color bgColor, System.Windows.Media.Color fgColor, int fontSize)
    {
        base.UpdateThemeSettingsControl(bgColor, fgColor, fontSize);

        var scale = PlotControl.DisplayScale;
        plotFontSize = (int)Math.Round(PlotFontSizeMultiplier * fontSize * scale);
        axesFontSize = (int)Math.Round(PlotFontSizeMultiplier * fontSize * scale);
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
        
        RestoreVerticalAxes();
        UpdateVerticalAxisTheme();
        
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

    private void InitializeRuntimeState()
    {
        ChartVariables ??= [];
        ChartVariableBindings ??= [];
        VerticalAxisBindings ??= [];
        VerticalAxes ??= [];
        currentColorIndex = Math.Max(currentColorIndex, 0);

        MouseMoveCommand = new RelayCommand<MouseEventArgs>(DisplayCursorBasedOnMouseMove);
        RemoveChartVariableCommand = new RelayCommand<object>(RemoveChartVariable);
        ClearGraphCommand = new RelayCommand<object>(ClearGraph);
        AddAxisCommand = new RelayCommand<object>(AddAxis);
        RemoveAxisCommand = new RelayCommand<object>(RemoveAxis);
        ExportImageCommand = new RelayCommand<object>(ExportImage);
        ExportCsvCommand = new RelayCommand<object>(ExportCsv);
        ZoomToFitCommand = new RelayCommand<object>((i) => PlotControl?.Plot.Axes.AutoScale());

        PlotControl = new WpfPlot();
        cross = PlotControl.Plot.Add.Crosshair(0, 0);
        annotation = PlotControl.Plot.Add.Annotation("", Alignment.UpperLeft);
        annotation.IsVisible = false;
        cross.IsVisible = IsCrossEnabled;
        PlotControl.Plot.Legend.Orientation = IsLegendHorizontal ? ScottPlot.Orientation.Horizontal : ScottPlot.Orientation.Vertical;

        // Remove context menu
        PlotControl.Menu?.Clear();
        PlotControl.UserInputProcessor.UserActionResponses.RemoveAll(
            x => x is ScottPlot.Interactivity.UserActionResponses.SingleClickContextMenu);

        PlotControl.Plot.Axes.Remove(Edge.Left);
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        InitializeRuntimeState();
    }

    [OnSerializing]
    private void OnSerializing(StreamingContext context)
    {
        SynchronizeVerticalAxisBindings();
        SynchronizeChartVariableBindings();
    }

    private System.Windows.Media.Color GetNextChartColor()
    {
        if (!ChartHelper.ChartColors.ContainsKey(currentColorIndex))
        {
            currentColorIndex = ChartHelper.ChartColors.Keys.Min();
        }

        var color = ChartHelper.ChartColors[currentColorIndex];
        currentColorIndex++;
        return color;
    }

    private ChartVariableBinding? GetSavedChartVariableBinding(IVariableBase variable)
    {
        return ChartVariableBindings.FirstOrDefault(b =>
            ControlBase.IsVariableReferenceMatch(b.VariableReference, variable));
    }

    private static System.Windows.Media.Color? GetSavedChartColor(ChartVariableBinding? binding)
    {
        if (binding?.TryGetChartColor(out var chartColor) == true)
        {
            return chartColor;
        }

        return null;
    }

    private static float GetSavedLineWidth(ChartVariableBinding? binding)
    {
        return binding?.LineWidth ?? 1.0f;
    }

    private void RememberChartVariableBinding(ChartVariable chartVariable)
    {
        if (chartVariable.Variable == null)
        {
            return;
        }

        var variableReference = GetVariableReference(chartVariable.Variable);
        var binding = ChartVariableBindings.FirstOrDefault(b => b.VariableReference == variableReference);
        if (binding == null)
        {
            ChartVariableBindings.Add(new ChartVariableBinding(
                variableReference,
                chartVariable.ChartColor,
                chartVariable.LineWidth,
                chartVariable.AxisIndex));
            return;
        }

        binding.SetChartColor(chartVariable.ChartColor);
        binding.LineWidth = chartVariable.LineWidth;
        binding.AxisIndex = chartVariable.AxisIndex;
    }

    private void SynchronizeChartVariableBindings()
    {
        if (ChartVariables.Count == 0)
        {
            return;
        }

        ChartVariableBindings = ChartVariables
            .Where(chartVariable => chartVariable.Variable != null)
            .Select(chartVariable => new ChartVariableBinding(
                GetVariableReference(chartVariable.Variable),
                chartVariable.ChartColor,
                chartVariable.LineWidth,
                chartVariable.AxisIndex))
            .ToList();
        LinkedVariables = ChartVariableBindings
            .Select(binding => binding.VariableReference)
            .ToList();
    }

    private void SynchronizeVerticalAxisBindings()
    {
        if (VerticalAxes.Count == 0)
        {
            return;
        }

        VerticalAxisBindings = VerticalAxes
            .OfType<VerticalAxis>()
            .Select(VerticalAxisBinding.FromAxis)
            .ToList();
    }

    private void RestoreVerticalAxes()
    {
        if (VerticalAxes.Count > 0)
        {
            return;
        }

        if (VerticalAxisBindings.Count == 0)
        {
            AddAxis(Edge.Left, 0);
            return;
        }

        for (var i = 0; i < VerticalAxisBindings.Count; i++)
        {
            AddAxis(VerticalAxisBindings[i], i);
        }
    }

    private int GetValidVerticalAxisIndex(int axisIndex)
    {
        RestoreVerticalAxes();
        if (VerticalAxes.Count == 0)
        {
            return -1;
        }

        return axisIndex >= 0 && axisIndex < VerticalAxes.Count
            ? axisIndex
            : 0;
    }

    private void UpdateVerticalAxisTheme()
    {
        foreach (var axis in VerticalAxes.OfType<VerticalAxis>())
        {
            axis.LabelBackgroundColor = backgroundColor;
            axis.LabelFontColor = foregroundColor;
            axis.LabelFontSize = plotFontSize;
            axis.LabelBorderColor = foregroundColor;
            axis.TickLabelStyle.ForeColor = foregroundColor;
            axis.TickLabelStyle.BackgroundColor = backgroundColor;
            axis.TickLabelStyle.FontSize = axesFontSize;
            axis.TickLabelStyle.PointColor = foregroundColor;
            axis.MajorTickStyle.Color = foregroundColor;
            axis.MinorTickStyle.Color = foregroundColor;
            axis.FrameLineStyle.Color = foregroundColor;
        }
    }

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
        MousePosition = p;
        ActualizeCrosshairAndAnnotation(p);
        PlotControl.Refresh();
    }
    
    private void ActualizeCrosshairAndAnnotation(Point p)
    {
        Pixel mousePixel = new(p.X * PlotControl.DisplayScale, p.Y * PlotControl.DisplayScale);
        Coordinates mouseCoordinates = PlotControl.Plot.GetCoordinates(mousePixel);
        var cursorX = GetCursorX(mouseCoordinates.X);

        cross.Position = new Coordinates(cursorX, mouseCoordinates.Y);

        var cursorValues = GetCursorValues(cursorX);
        UpdateLegendTexts(cursorValues);
    }

    private double GetCursorX(double mouseX)
    {
        var chartVariable = SelectedChartVariable?.IsVisible == true && HasCursorData(SelectedChartVariable)
            ? SelectedChartVariable
            : ChartVariables.FirstOrDefault(v => v.IsVisible && HasCursorData(v));

        if (chartVariable == null)
        {
            return mouseX;
        }

        var index = GetNearestXIndex(chartVariable.XVal, mouseX);
        return chartVariable.XVal[index];
    }

    private List<CursorValue> GetCursorValues(double cursorX)
    {
        var cursorValues = new List<CursorValue>();

        foreach (var chartVariable in ChartVariables.Where(v => v.IsVisible && HasCursorData(v)))
        {
            var index = GetNearestXIndex(chartVariable.XVal, cursorX);
            cursorValues.Add(new CursorValue(chartVariable, chartVariable.XVal[index], chartVariable.YVal[index]));
        }

        return cursorValues;
    }

    private static bool HasCursorData(ChartVariable chartVariable)
    {
        return chartVariable.XVal.Count > 0 && chartVariable.XVal.Count == chartVariable.YVal.Count;
    }

    private static int GetNearestXIndex(List<double> values, double x)
    {
        var index = values.BinarySearch(x);
        if (index >= 0)
        {
            return index;
        }

        index = ~index;
        if (index <= 0)
        {
            return 0;
        }

        if (index >= values.Count)
        {
            return values.Count - 1;
        }

        return Math.Abs(values[index] - x) < Math.Abs(values[index - 1] - x)
            ? index
            : index - 1;
    }

    private static string GetCursorLabel(ChartVariable chartVariable)
    {
        return GetVariableLegendText(chartVariable.Variable);
    }

    private void UpdateLegendTexts(IReadOnlyCollection<CursorValue> cursorValues)
    {
        foreach (var chartVariable in ChartVariables)
        {
            if (chartVariable.ChartSignal == null)
            {
                continue;
            }

            var cursorValue = cursorValues.FirstOrDefault(v => ReferenceEquals(v.ChartVariable, chartVariable));
            chartVariable.ChartSignal.LegendText = cursorValue.ChartVariable == null
                ? GetCursorLabel(chartVariable)
                : $"{GetCursorLabel(chartVariable)} [{Math.Round(cursorValue.X, 3)}; {Math.Round(cursorValue.Y, 3)}]";
        }
    }

    private void ResetLegendTexts()
    {
        foreach (var chartVariable in ChartVariables.Where(v => v.Variable != null))
        {
            if (chartVariable.ChartSignal != null)
            {
                chartVariable.ChartSignal.LegendText = GetCursorLabel(chartVariable);
            }
        }
    }

    private static string GetVariableLegendText(IVariableBase variable)
    {
        var label = string.IsNullOrWhiteSpace(variable.Label)
            ? variable.Name
            : variable.Label;

        return $"{label} ({variable.Id})";
    }

    private readonly record struct CursorValue(ChartVariable ChartVariable, double X, double Y);

    private void ExportImage(object parameter)
    {
        var dialog = CreateSaveFileDialog(
            "PNG image (*.png)|*.png|JPEG image (*.jpg)|*.jpg|BMP image (*.bmp)|*.bmp|WebP image (*.webp)|*.webp|SVG image (*.svg)|*.svg",
            "graph.png");

        dialog.ShowDialog();
        if (dialog.DialogResult != true)
        {
            return;
        }

        var filePath = EnsureFileExtension(dialog.FileName, ".png");
        RememberSaveDialogDirectory(filePath);
        var width = GetExportPixelWidth();
        var height = GetExportPixelHeight();

        switch (Path.GetExtension(filePath).ToLowerInvariant())
        {
            case ".jpg":
            case ".jpeg":
                PlotControl.Plot.SaveJpeg(filePath, width, height);
                break;
            case ".bmp":
                PlotControl.Plot.SaveBmp(filePath, width, height);
                break;
            case ".webp":
                PlotControl.Plot.SaveWebp(filePath, width, height);
                break;
            case ".svg":
                PlotControl.Plot.SaveSvg(filePath, width, height);
                break;
            default:
                PlotControl.Plot.SavePng(filePath, width, height);
                break;
        }
    }

    private void ExportCsv(object parameter)
    {
        var dialog = CreateSaveFileDialog("CSV files (*.csv)|*.csv", "graph-data.csv");

        dialog.ShowDialog();
        if (dialog.DialogResult != true)
        {
            return;
        }

        var filePath = EnsureFileExtension(dialog.FileName, ".csv");
        RememberSaveDialogDirectory(filePath);
        File.WriteAllText(filePath, CreateCsv(), Encoding.UTF8);
    }

    private string CreateCsv()
    {
        var csv = new StringBuilder();
        csv.AppendLine("VariableId,VariableName,Index,Timestamp,X,Y");

        foreach (var chartVariable in ChartVariables.Where(v => v.Variable != null))
        {
            var count = Math.Min(chartVariable.XVal.Count, chartVariable.YVal.Count);
            for (var i = 0; i < count; i++)
            {
                var timestamp = i < chartVariable.XDateTimeVal.Count
                    ? chartVariable.XDateTimeVal[i].ToString("O", CultureInfo.InvariantCulture)
                    : string.Empty;

                csv.Append(chartVariable.Variable.Id.ToString(CultureInfo.InvariantCulture));
                csv.Append(',');
                csv.Append(EscapeCsv(GetVariableLegendText(chartVariable.Variable)));
                csv.Append(',');
                csv.Append(i.ToString(CultureInfo.InvariantCulture));
                csv.Append(',');
                csv.Append(EscapeCsv(timestamp));
                csv.Append(',');
                csv.Append(chartVariable.XVal[i].ToString("G17", CultureInfo.InvariantCulture));
                csv.Append(',');
                csv.Append(chartVariable.YVal[i].ToString("G17", CultureInfo.InvariantCulture));
                csv.AppendLine();
            }
        }

        return csv.ToString();
    }

    private int GetExportPixelWidth()
    {
        var width = PlotControl.ActualWidth > 0 ? PlotControl.ActualWidth : Width;
        return Math.Max(1, (int)Math.Round(width * PlotControl.DisplayScale));
    }

    private int GetExportPixelHeight()
    {
        var height = PlotControl.ActualHeight > 0 ? PlotControl.ActualHeight : Height;
        return Math.Max(1, (int)Math.Round(height * PlotControl.DisplayScale));
    }

    private RadSaveFileDialog CreateSaveFileDialog(string filter, string fileName)
    {
        var dialog = new RadSaveFileDialog()
        {
            Owner = Application.Current?.MainWindow,
            Filter = filter,
            FileName = fileName,
            InitialDirectory = GetSaveDialogInitialDirectory()
        };

        ConfigureSaveFileDialog?.Invoke(dialog);
        return dialog;
    }

    private string GetSaveDialogInitialDirectory()
    {
        var initialDirectory = SaveDialogInitialDirectoryProvider?.Invoke();
        if (Directory.Exists(initialDirectory))
        {
            return initialDirectory;
        }

        if (Directory.Exists(Environment.CurrentDirectory))
        {
            return Environment.CurrentDirectory;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    private void RememberSaveDialogDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            SaveDialogDirectoryChanged?.Invoke(directory);
        }
    }

    private static string EnsureFileExtension(string filePath, string defaultExtension)
    {
        return string.IsNullOrWhiteSpace(Path.GetExtension(filePath))
            ? $"{filePath}{defaultExtension}"
            : filePath;
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains('"') && !value.Contains(',') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
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
            RemoveChartVariableBinding(cVar);
            if (ChartVariables.Count > 0)
            {
                SelectedChartVariable = ChartVariables[Math.Min(index, ChartVariables.Count - 1)];
            }
            PlotControl.Refresh();
        }
    }

    private void RemoveChartVariableBinding(IVariableBase variable)
    {
        var variableReference = GetVariableReference(variable);
        LinkedVariables.RemoveAll(v => v == variableReference || ControlBase.IsVariableReferenceMatch(v, variable));
        ChartVariableBindings.RemoveAll(v => v.VariableReference == variableReference || ControlBase.IsVariableReferenceMatch(v.VariableReference, variable));
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
        AddAxis(edge, index, null);
    }

    private void AddAxis(VerticalAxisBinding axisBinding, int index)
    {
        AddAxis(axisBinding.Edge, index, axisBinding);
    }

    private void AddAxis(Edge edge, int index, VerticalAxisBinding? axisBinding)
    {
        var newAxis = new VerticalAxis(edge)
        {
            Name = string.IsNullOrWhiteSpace(axisBinding?.Name) ? $"Y->{index}" : axisBinding.Name,
            IsVisible = axisBinding?.IsVisible ?? true,
            IsAutoScale = axisBinding?.IsAutoScale ?? true,
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
        VerticalAxes.Add(newAxis);

        newAxis.Minimum = axisBinding?.Minimum ?? -10.0;
        newAxis.Maximum = axisBinding?.Maximum ?? 10.0;
        PlotControl.Plot.Axes.SetLimitsY(bottom: newAxis.Minimum, top: newAxis.Maximum, yAxis: newAxis);
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
