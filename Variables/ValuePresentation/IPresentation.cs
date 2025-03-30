namespace Qenex.QSuite.Variables.ValuePresentation;

public interface IPresentation
{
    string Name { get; set; }
    string Label { get; set; }
    double Min { get; set; }
    double Max { get; set; }
    string PrintFormat { get; set; }
    string Unit { get; set; }
}