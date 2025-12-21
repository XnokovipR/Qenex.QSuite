using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Base interface for all controls which are used in workspaces.
/// </summary>
public interface IControlBase
{
	//todo: IControl - add acceptable variable types
	//todo: IControl - add Variables (protocol or classical)

	/// <summary>
	/// Full name of the control.
	/// </summary>
	string ControlName { get; }
	
	/// <summary>
	/// Simplified label of the control showed in Controls Toolbox.
	/// </summary>
	string Label { get; }
	
	/// <summary>
	/// Icon of the control showed in Controls Toolbox.
	/// </summary>
	BitmapImage Icon { get; }
	
	/// <summary>
	/// Description of the control showed in Controls Toolbox.
	/// </summary>
	string Description { get; }
	
	/// <summary>
	/// Minimum height of the control.
	/// </summary>
	int MinHeight { get; }
	
	/// <summary>
	/// Minimum width of the control.
	/// </summary>
	int MinWidth { get; }
	

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


	/// <summary>
	/// Enable or disable connectors.
	/// </summary>
	bool AreConnectorsEnabled { get; set; }

	/// <summary>
	/// Background color of the control.
	/// </summary>
	Color BackgroundColor { get; set; }
	
	/// <summary>
	/// Color of the label text background.
	/// </summary>
	Color LabelBackgroundColor { get; set; }
	
	/// <summary>
	/// Color of the label text.
	/// </summary>
	Color LabelColor { get; set; }
	
	/// <summary>
	/// Indicates whether the control is locked for editing (moving, resizing, etc.).
	/// </summary>
	bool IsLocked { get; set; }
}