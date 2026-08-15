using System.Runtime.Serialization;
using System.Windows.Media;
using Edge = ScottPlot.Edge;

namespace Qenex.QSuite.Controls.XYGraphControl.ViewModels;

[DataContract]
public class VerticalAxisBinding
{
    [DataMember]
    public string Name { get; set; } = string.Empty;

    [DataMember]
    public string? Label { get; set; }

    [DataMember]
    public Edge Edge { get; set; } = Edge.Right;

    [DataMember]
    public bool IsVisible { get; set; } = true;

    [DataMember]
    public bool IsAutoScale { get; set; } = true;

    [DataMember]
    public double Minimum { get; set; } = -10.0;

    [DataMember]
    public double Maximum { get; set; } = 10.0;

    [DataMember]
    public string? AxisColor { get; set; }

    public void SetAxisColor(Color? axisColor)
    {
        AxisColor = axisColor.HasValue
            ? $"{axisColor.Value.R},{axisColor.Value.G},{axisColor.Value.B}"
            : null;
    }

    public bool TryGetAxisColor(out Color axisColor)
    {
        axisColor = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(AxisColor))
        {
            return false;
        }

        var parts = AxisColor.Split(',');
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

        axisColor = Color.FromRgb(r, g, b);
        return true;
    }

    public static VerticalAxisBinding FromAxis(VerticalAxis axis)
    {
        var binding = new VerticalAxisBinding
        {
            Name = axis.Name,
            Label = axis.AxisLabel,
            Edge = axis.Edge,
            IsVisible = axis.IsVisible,
            IsAutoScale = axis.IsAutoScale,
            Minimum = axis.Minimum,
            Maximum = axis.Maximum
        };
        binding.SetAxisColor(axis.AxisColor);
        return binding;
    }
}
