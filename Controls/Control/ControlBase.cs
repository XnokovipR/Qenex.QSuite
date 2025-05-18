using System.Windows.Media.Imaging;

namespace Qenex.QSuite.Controls.Control;

public abstract class ControlBase : IControlBase
{
	#region Private fiedlds

	private const int DefaultHeight = 100;
	private const int DefaultWidth = 100;

	#endregion

	#region Constructors
	public ControlBase()
	{
		Tags = [];
	}

	#endregion

	#region Properties
	public abstract string ControlName { get; }
	public abstract string Label { get; }
	public abstract BitmapImage Icon { get; }
	public abstract string Description { get; }
	public virtual int MinHeight { get; } = DefaultHeight;
	public virtual int MinWidth { get; } = DefaultWidth;

	/// <summary>
	/// Tags are used to store custom user control properties.
	/// </summary>
	public Dictionary<string, object> Tags { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
	public int Width { get; set; } = DefaultWidth;
	public int Height { get; set; } = DefaultHeight;

	#endregion
}