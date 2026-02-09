using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Telerik.Windows.Controls;

namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Base interface for all controls which are used in workspaces.
/// </summary>
public interface IControlBase
{

	#region Properties
	
	/// <summary>
	/// Diagram shape associated with the control.
	/// </summary>
	RadDiagramShape? DiagramShape { get; set; }
	
	/// <summary>
	/// Identifier of the control.
	///	 </summary>
	int Id { get; set; }
	
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
	/// Foreground color of the control.
	/// </summary>
	Color ForegroundColor { get; set; }
	
	/// <summary>
	/// Indicates whether the control is locked for editing (moving, resizing, etc.).
	/// </summary>
	bool IsLocked { get; set; }
	bool IsRun { get; set; }
	
	/// <summary>
	/// Variables binded to the control.
	/// </summary>
	ObservableCollection<IVariableBase> Variables { get; set; }
		
	#endregion

	#region Public methods
	
	void UpdateColorControl(Color backgroundColor, Color foregroundColor);

	Task UpdateVariableValueAsync(IVariableBase variable);
	
	void BindVariable(IVariableBase protVariable);

	#endregion
}