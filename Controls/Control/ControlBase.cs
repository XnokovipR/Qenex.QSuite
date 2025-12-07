
using Qenex.QLibs.QUI;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Qenex.QSuite.Controls.Control;

public abstract class ControlBase : PropertyChangedBaseWithValidation, IControlBase
{
	private int x;
	private int y;
	private int width;
	private int height;
	private bool areConnectorsEnabled;
	private Color backgroundColor;

	protected ControlBase()
	{
		Width = MinWidth;
		Height = MinHeight;
		AreConnectorsEnabled = false;
	}

	public abstract string ControlName { get; }

	public abstract string Label { get; }

	public abstract BitmapImage Icon { get; }

	public abstract string Description { get; }

	public virtual int MinHeight => 50;

	public virtual int MinWidth => 100;

	public int X { get => x; set { x = value; OnPropertyChanged(); } }
	public int Y { get => y; set { y = value; OnPropertyChanged(); } }
	public int Width { get => width; set { width = value; OnPropertyChanged(); } }
	public int Height { get => height; set { height = value; OnPropertyChanged(); } }
	public bool AreConnectorsEnabled { get => areConnectorsEnabled; set { areConnectorsEnabled = value; OnPropertyChanged(); } }
	public Color BackgroundColor { get => backgroundColor; set { backgroundColor = value; OnPropertyChanged(); } }
}