using Telerik.Windows.Controls;

namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Control, ktery pro export/ukladani pouziva save-file dialog konfigurovany hostem (QInsight).
/// Umoznuje hostovi predat callbacky bez znalosti konkretniho typu controlu (plugin model).
/// </summary>
public interface IFileDialogAwareControl
{
	/// <summary>
	/// Host konfiguruje save-file dialog (napr. filtry, vychozi nazev).
	/// </summary>
	Action<DialogWindowBase>? ConfigureSaveFileDialog { get; set; }

	/// <summary>
	/// Poskytuje vychozi adresar pro save-file dialog.
	/// </summary>
	Func<string?>? SaveDialogInitialDirectoryProvider { get; set; }

	/// <summary>
	/// Notifikace hostu, ze uzivatel zmenil adresar v dialogu.
	/// </summary>
	Action<string>? SaveDialogDirectoryChanged { get; set; }
}
