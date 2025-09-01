using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Controls.Control;

public abstract class ControlBase : PropertyChangedBaseWithValidation, IControlBase
{
	#region Fields
	
	private const int DefaultHeight = 75;
	private const int DefaultWidth = 100;

	private int height = DefaultHeight;
	private int width = DefaultWidth;
	private int x;
	private int y;
	private Dictionary<string, object> tags { get; set; }

	#endregion

	#region Constructors
	public ControlBase()
	{
		Tags = [];
		Variables = new ObservableCollection<IControlVariable>();
	}

	#endregion

	#region Properties
	public abstract string ControlName { get; }
	public abstract string Label { get; }
	public abstract BitmapImage Icon { get; }
	public abstract string Description { get; }
	public virtual int MinHeight { get; } = DefaultHeight;
	public virtual int MinWidth { get; } = DefaultWidth;

	public ObservableCollection<IControlVariable> Variables { get; set; }
	
	/// <summary>
	/// Tags are used to store custom user control properties.
	/// </summary>
	public Dictionary<string, object> Tags { get; set; }

	public int X { get => x; set { x = value; OnPropertyChanged(); } }

	public int Y { get => y; set { y = value; OnPropertyChanged(); } }

	public int Width { get => width; set { width = value; OnPropertyChanged(); } }
	public int Height { get => height; set { height = value; OnPropertyChanged(); } }

	public bool AreConnectorsEnabled { get; set; } = true;

	#endregion
}