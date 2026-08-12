using System.Runtime.Serialization;
using System.Windows.Media;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

[DataContract]
public class ChartVariableBinding
{
    [DataMember]
    public string VariableReference { get; set; } = string.Empty;

    [DataMember]
    public string ChartColor { get; set; } = string.Empty;

    [DataMember]
    public float LineWidth { get; set; } = 1.0f;

    [DataMember]
    public ChartLineStyle LineStyle { get; set; } = ChartLineStyle.Solid;

    [DataMember]
    public int AxisIndex { get; set; }

    public ChartVariableBinding()
    {
    }

    public ChartVariableBinding(string variableReference, Color chartColor, float lineWidth = 1.0f, int axisIndex = 0,
        ChartLineStyle lineStyle = ChartLineStyle.Solid)
    {
        VariableReference = variableReference;
        LineWidth = lineWidth;
        AxisIndex = axisIndex;
        LineStyle = lineStyle;
        SetChartColor(chartColor);
    }

    public void SetChartColor(Color chartColor)
    {
        ChartColor = $"{chartColor.R},{chartColor.G},{chartColor.B}";
    }

    public bool TryGetChartColor(out Color chartColor)
    {
        chartColor = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(ChartColor))
        {
            return false;
        }

        var parts = ChartColor.Split(',');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!byte.TryParse(parts[0], out var r) ||
            !byte.TryParse(parts[1], out var g) ||
            !byte.TryParse(parts[2], out var b))
        {
            return false;
        }

        chartColor = Color.FromRgb(r, g, b);
        return true;
    }

    [OnDeserializing]
    private void OnDeserializing(StreamingContext context)
    {
        VariableReference = string.Empty;
        ChartColor = string.Empty;
        LineWidth = 1.0f;
        LineStyle = ChartLineStyle.Solid;
        AxisIndex = 0;
    }
}
