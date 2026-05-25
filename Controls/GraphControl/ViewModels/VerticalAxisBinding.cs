using System.Runtime.Serialization;
using ScottPlot;

namespace Qenex.QSuite.Controls.GraphControl.ViewModels;

[DataContract]
public class VerticalAxisBinding
{
    [DataMember]
    public string Name { get; set; } = string.Empty;

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

    public static VerticalAxisBinding FromAxis(VerticalAxis axis)
    {
        return new VerticalAxisBinding
        {
            Name = axis.Name,
            Edge = axis.Edge,
            IsVisible = axis.IsVisible,
            IsAutoScale = axis.IsAutoScale,
            Minimum = axis.Minimum,
            Maximum = axis.Maximum
        };
    }
}
