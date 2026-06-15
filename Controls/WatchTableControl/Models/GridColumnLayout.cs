using System.Runtime.Serialization;

namespace Qenex.QSuite.Controls.WatchTableControl.Models;

/// <summary>Ulozeny layout jednoho sloupce gridu (poradi, sirka, viditelnost) - serializuje se do projektu.</summary>
[DataContract]
public sealed class GridColumnLayout
{
	[DataMember]
	public string UniqueName { get; set; } = string.Empty;

	[DataMember]
	public int DisplayIndex { get; set; }

	[DataMember]
	public double Width { get; set; }

	[DataMember]
	public bool IsVisible { get; set; } = true;
}
