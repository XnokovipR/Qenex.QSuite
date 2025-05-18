using System.Windows.Media.Imaging;

namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Base interface for all controls which are used in workspaces.
/// </summary>
public interface IControlBase
{
	//todo: IControlBase - add acceptable variable types
	//todo: IControlBase - add Variables (protocol or classical)

	string ControlName { get; }
	string Label { get; }
	BitmapImage Icon { get; }
	string Description { get; }
	int MinHeight { get; }
	int MinWidth { get; }


	/// <summary>
	/// Custom user control properties.
	/// </summary>
	Dictionary<string, object> Tags { get; set; }

	/// <summary>
	/// Control horizontal position of the left upper corner.
	/// </summary>
	int X { get; set; }

	/// <summary>
	/// Control vertical position of the left upper corner.
	/// </summary>
	int Y { get; set; }

	/// <summary>
	/// Control width.
	/// </summary>
	int Width { get; set; }

	/// <summary>
	/// Control height.
	/// </summary>
	int Height { get; set; }
}