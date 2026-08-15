namespace Qenex.QSuite.Controls.XYGraphControl.Models;

public static class ColorExtensions
{
    public static ScottPlot.Color ToScottPlotColor(this System.Windows.Media.Color color)
    {
        return new ScottPlot.Color(color.R, color.G, color.B, color.A);
    }    
}