using RtGraphControl.Models;
using ScottPlot;
using ScottPlot.AxisPanels;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

public sealed class VerticalAxis : YAxisBase
{
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
    }
}