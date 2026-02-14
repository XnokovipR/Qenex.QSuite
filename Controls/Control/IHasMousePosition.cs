using System.Windows;

namespace Qenex.QSuite.Controls.Control;

public interface IHasMousePosition
{
    Point MousePosition { get; set; }
}