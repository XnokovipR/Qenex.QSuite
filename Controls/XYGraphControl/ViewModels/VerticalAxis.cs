using System.ComponentModel;
using Qenex.QSuite.Controls.XYGraphControl.Models;
using ScottPlot;
using ScottPlot.AxisPanels;

namespace Qenex.QSuite.Controls.XYGraphControl.ViewModels;

public sealed class VerticalAxis : YAxisBase, INotifyPropertyChanged
{
    // The axes settings grid binds to Minimum/Maximum; without change notification it would keep
    // showing the values from binding time while autoscale moves the real limits underneath.
    public event PropertyChangedEventHandler? PropertyChanged;

    public Action? RefreshAction;
    public VerticalAxis(Edge edge = Edge.Right)
    {
        this.edge = edge;

        TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic();
        LabelRotation = -90;
        LabelBold = false;
    }

    private Edge edge;

    public override Edge Edge => edge;

    // AxisBase.Edge je abstraktni get-only, binding z UI jde pres tuto property
    public Edge EdgeSelection
    {
        get => edge;
        set
        {
            edge = value;
            RefreshAction?.Invoke();
        }
    }

    // Label ma cist zdola nahoru na OBOU stranach (rotace -90). Zakladni Render ale
    // kotvi label UpperCenter, coz s rotaci -90 na prave ose vykresli text za okraj
    // okna - proto pravou osu kreslime sami s kotvou LowerCenter (sklopi text dovnitr).
    public override void Render(RenderPack rp, float size, float offset)
    {
        if (Edge == Edge.Left)
        {
            base.Render(rp, size, offset);
            return;
        }

        if (!IsVisible)
        {
            return;
        }

        var panelRect = GetPanelRect(rp.DataRect, size, offset, rp.Paint);
        var labelPoint = new Pixel(panelRect.Right - PaddingOutsideAxisLabels.Horizontal, rp.DataRect.VerticalCenter);

        LabelAlignment = Alignment.LowerCenter;

        rp.CanvasState.Save();
        if (ClipLabel)
        {
            rp.CanvasState.Clip(panelRect);
        }

        LabelStyle.Render(rp.Canvas, labelPoint, rp.Paint);
        rp.CanvasState.Restore();

        DrawTicks(rp, TickLabelStyle, panelRect, TickGenerator.Ticks, this, MajorTickStyle, MinorTickStyle);
        DrawFrame(rp, panelRect, Edge, FrameLineStyle);
    }

    public static IReadOnlyList<Edge> AvailableEdges { get; } = [Edge.Left, Edge.Right];

    // True for the fixed X row (row 1 of the axes grid): configuration only, never registered
    // in the plot — its values drive Plot.Axes.Bottom instead of drawing a vertical ruler.
    public bool IsXAxisRow { get; init; }

    public bool IsEdgeSelectable => !IsXAxisRow;

    // Shadows AxisBase.IsVisible so a grid edit raises change notification and a redraw
    // (for the X row RefreshAction also propagates the value to the bottom axis).
    public new bool IsVisible
    {
        get => base.IsVisible;
        set
        {
            base.IsVisible = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            RefreshAction?.Invoke();
        }
    }

    public string Name { get; set; } = string.Empty;

    // ScottPlot renders no label (and reserves no space) for empty LabelText;
    // rotation and colors come from LabelRotation and ApplyColor.
    public string AxisLabel
    {
        get => LabelText;
        set
        {
            LabelText = value;
            RefreshAction?.Invoke();
        }
    }


    public bool IsAutoScale { get; set; } = true;

    // Autoscale fit runs on the sample stream: it must not fire RefreshAction (an unthrottled
    // redraw per change) — the caller refreshes on its own throttle.
    private bool suppressRefreshAction;

    /// <summary>Sets both limits without invoking RefreshAction (PropertyChanged still fires
    /// so the axes grid shows the live values). Unchanged values are skipped entirely — a
    /// steady fit then causes no grid updates at all (no cell/column-width churn). Returns
    /// true when a limit actually changed, so the caller only re-applies the axis then and
    /// never fights the user's mouse zoom with a per-refresh slam.</summary>
    public bool SetRangeQuiet(double min, double max)
    {
        var changed = false;
        suppressRefreshAction = true;
        try
        {
            if (Minimum != min)
            {
                Minimum = min;
                changed = true;
            }

            if (Maximum != max)
            {
                Maximum = max;
                changed = true;
            }
        }
        finally
        {
            suppressRefreshAction = false;
        }

        return changed;
    }

    public double Minimum
    {
        get;
        set
        {
            field = value;
            Min = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Minimum)));
            if (!suppressRefreshAction)
            {
                RefreshAction?.Invoke();
            }
        }
    } = -10.0;

    public double Maximum
    {
        get;
        set
        {
            field = value;
            Max = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Maximum)));
            if (!suppressRefreshAction)
            {
                RefreshAction?.Invoke();
            }
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
    }
}