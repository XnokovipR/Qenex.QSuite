
using Qenex.QLibs.QUI;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.QVariables;
using Telerik.Windows.Controls;

namespace Qenex.QSuite.Controls.Control;

public abstract class ControlBase : PropertyChangedBaseWithValidation, IControlBase
{
	protected ControlBase()
	{
		Width = MinWidth;
		Height = MinHeight;
		AreConnectorsEnabled = false;
		Variables = [];
	}

	#region Properties
	
	public int Id { get; set; }
	
	public RadDiagramShape? DiagramShape { get; set; }
	
	public abstract string ControlName { get; }

	public abstract string Label { get; }

	public abstract BitmapImage Icon { get; }

	public abstract string Description { get; }

	public int MinHeight => 50;

	public int MinWidth => 100;

	public int X { get; set { field = value; OnPropertyChanged(); } }
	public int Y { get; set { field = value; OnPropertyChanged(); } }
	public int Width { get; set { field = value; OnPropertyChanged(); } }
	public int Height { get; set { field = value; OnPropertyChanged(); } }
	public bool AreConnectorsEnabled { get; set { field = value; OnPropertyChanged(); } }

	public Color BackgroundColor
	{
		get;
		set
		{
			field = value;
			OnPropertyChanged();
			DiagramShape?.Background = new SolidColorBrush(value);
		}
	} = Colors.White;//Color.FromRgb(90, 90, 90);

	public Color ForegroundColor
	{
		get;
		set { field = value; OnPropertyChanged(); }
	}

	public bool IsLocked { get; set { field = value; OnPropertyChanged(); } }
	public bool IsRun { get; set { field = value; OnPropertyChanged(); } }
	public List<IVariableBase> Variables { get; set { field = value; OnPropertyChanged(); } }

	#endregion

	#region Public methods

	public abstract Task UpdateVariableValueAsync(IVariableBase variable);
	public abstract void BindVariable(IVariableBase protVariable);
	
	public virtual void UpdateColorControl(Color backgroundColor, Color foregroundColor)
	{
		BackgroundColor = backgroundColor;
		ForegroundColor = foregroundColor;
	}

	#endregion
}