using System.Runtime.Serialization;

namespace Qenex.QSuite.Controls.WatchTableControl.Models;

/// <summary>Saved layout of one grid column (order, width, visibility) - serialized into the project.</summary>
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
