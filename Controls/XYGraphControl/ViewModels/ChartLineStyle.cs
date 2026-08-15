using ScottPlot;

namespace Qenex.QSuite.Controls.XYGraphControl.ViewModels;

/// <summary>
/// Line patterns ScottPlot defines out of the box (LinePattern presets), as a
/// serializable enum. Solid must stay the first member — projects saved before
/// this property existed deserialize to the enum default.
/// </summary>
public enum ChartLineStyle
{
    Solid,
    Dashed,
    DenselyDashed,
    Dotted,
}

public static class ChartLineStyleExtensions
{
    public static LinePattern ToLinePattern(this ChartLineStyle style) => style switch
    {
        ChartLineStyle.Dashed => LinePattern.Dashed,
        ChartLineStyle.DenselyDashed => LinePattern.DenselyDashed,
        ChartLineStyle.Dotted => LinePattern.Dotted,
        _ => LinePattern.Solid,
    };
}
