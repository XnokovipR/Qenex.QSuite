
using Qenex.QLibs.QUI;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Qenex.QSuite.Controls.Control;

public abstract class ControlBase : PropertyChangedBaseWithValidation, IControlBase
{
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

	public int MinHeight => 50;

	public int MinWidth => 100;

	public int X { get; set { field = value; OnPropertyChanged(); } }
	public int Y { get; set { field = value; OnPropertyChanged(); } }
	public int Width { get; set { field = value; OnPropertyChanged(); } }
	public int Height { get; set { field = value; OnPropertyChanged(); } }
	public bool AreConnectorsEnabled { get; set { field = value; OnPropertyChanged(); } }
	public Color BackgroundColor { get; set { field = value; OnPropertyChanged(); } }
	public Color LabelBackgroundColor { get; set { field = value; OnPropertyChanged(); } }
	public Color LabelColor { get; set { field = value; OnPropertyChanged(); } }
	public bool IsLocked { get; set { field = value; OnPropertyChanged(); } }
}