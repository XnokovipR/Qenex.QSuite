namespace Qenex.QSuite.QVariables.Presentation;

public class Presentation : IPresentation
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Min { get; set; }
    public int Max { get; set; }
    public string PrintFormat { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}