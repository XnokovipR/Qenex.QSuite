namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Control, ktery vedle <see cref="IControlBase.Variables"/> drzi dalsi reference na promenne
/// (napr. graf s vice navazanymi prubehy). Host (QInsight) pouziva tyto reference pri zjistovani,
/// zda je promenna jeste pouzita, bez znalosti konkretniho typu controlu (plugin model).
/// </summary>
public interface IVariableReferenceProvider
{
	IEnumerable<string> GetAdditionalVariableReferences();
}
