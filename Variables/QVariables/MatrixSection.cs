#nullable enable
using Qenex.QSuite.Variables.QVariables.Values;
using Qenex.QSuite.Variables.ValuePresentation;

namespace Qenex.QSuite.Variables.QVariables;

/// <summary>
/// One section of a MatrixVariable layout (an axis or the data block).
/// Counts are numbers of values, never bytes; byte sizes and offsets are derived
/// from the element data types by MatrixVariable.
/// </summary>
public class MatrixSection
{
    /// <summary>
    /// Number of values in the section. For the data section it is only meaningful
    /// when the matrix has no axes; with axes the data count is derived from them.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Element type of the section; Undefined falls back to MatrixVariable.DefaultDataType.
    /// </summary>
    public ValuesGlobal.ValueDataType DataType { get; set; } = ValuesGlobal.ValueDataType.Undefined;

    /// <summary>
    /// Presentation (conversion, print format, unit) applied to the section values; optional.
    /// </summary>
    public IPresentation? Presentation { get; set; }
}
