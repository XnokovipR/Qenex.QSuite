using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Qenex.QSuite.Controls.WatchTableControl.Models;

/// <summary>Jeden radek watch table - jedna napojena promenna.</summary>
public sealed class WatchRow : INotifyPropertyChanged
{
	/// <summary>Reference na promennou (ControlBase.GetVariableReference) - identifikuje radek.</summary>
	public string Reference { get; init; } = string.Empty;

	public string Name { get; set { field = value; OnChanged(); } } = string.Empty;
	public string Unit { get; set { field = value; OnChanged(); } } = string.Empty;
	public string Value { get; set { field = value; OnChanged(); } } = string.Empty;
	public DateTime Time { get; set { field = value; OnChanged(); } }

	/// <summary>Posledni cas aktualizace - pro throttle (RefreshTime).</summary>
	internal DateTime LastUpdate { get; set; } = DateTime.MinValue;

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
