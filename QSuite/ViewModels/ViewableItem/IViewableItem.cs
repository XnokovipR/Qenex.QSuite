using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Qenex.QSuite.ViewModels.ViewableItem
{
	public interface IViewableItem
	{
		string Label { get; set; }
		BitmapImage Icon { get; }
		ObservableCollection<IViewableItem> Children { get; set; }
	}
}
