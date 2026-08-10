namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Control, ktery hlasi vyznamne runtime udalosti do aplikacniho logu. Hostitel (QInsight)
/// dodava sinky pri konfiguraci controlu (plugin model - bez zavislosti controlu na
/// LogSystems); nenastavene sinky znamenaji tichy provoz.
/// </summary>
public interface ILogAwareControl
{
	/// <summary>
	/// Informacni zaznam - ocekavane udalosti (napr. restart casove osy pri replayi).
	/// </summary>
	Action<string>? LogInfo { get; set; }

	/// <summary>
	/// Varovani - nesrovnalosti, ktere control sam zkorigoval (napr. couvnuti timestampu).
	/// </summary>
	Action<string>? LogWarn { get; set; }
}
