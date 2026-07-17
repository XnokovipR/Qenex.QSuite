using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Control writing single elements (cells) of a matrix variable to the device. Extends the
/// scalar write contract: the host injects the element write delegate the same way, and the
/// control stays ignorant of protocols/drivers. The base IVariableWriteControl members keep
/// their meaning (CanWriteVariableProvider gates the write mode); the scalar
/// WriteVariableEngValueAsync delegate is unused by matrix controls.
/// </summary>
public interface IMatrixVariableWriteControl : IVariableWriteControl
{
    /// <summary>
    /// Host injektuje: zapise inzenyrskou hodnotu jednoho prvku matice (sekce + index) do
    /// zarizeni (inverzni konverze, fronta zapisu a notifikace command driveru na strane hosta).
    /// </summary>
    Func<IVariableBase, MatrixSectionKind, int, double, Task<bool>>? WriteMatrixElementEngValueAsync { get; set; }
}
