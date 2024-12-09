namespace Qenex.QSuite.QVariables.Presentation;

public interface IPresentation
{
    string Name { get; set; }
    string Label { get; set; }
    int Min { get; set; }
    int Max { get; set; }
    string PrintFormat { get; set; }
    string Unit { get; set; }
}