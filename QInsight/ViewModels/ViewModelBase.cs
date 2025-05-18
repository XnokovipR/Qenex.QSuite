using Qenex.QLibs.QUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qenex.QInsight.ViewModels;

public abstract class ViewModelBase(EventAggregator ea) : PropertyChangedBaseWithValidation, IViewModelBase
{
	protected readonly EventAggregator EventAggregator = ea;

	#region Inherited from IToolViewModel (derived from IViewModelBase, IViewModelExtData)

	public abstract string Header { get; set; }
	public abstract string Name { get; set; }
	public abstract DockingPosition DockPosition { get; set; }
	public abstract bool IsDocument { get; }
	public Dictionary<string, object> CustomTags { get; set; } = new();

	public abstract void Exit();

	#endregion
}