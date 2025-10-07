using System.Windows;
using Qenex.QLibs.QUI;
using Qenex.QLibs.QUI.TelerikDocking;

namespace Qenex.QInsight.ViewModels;

public abstract class WorkspaceViewModelBase(EventAggregator ea) : PropertyChangedBaseWithValidation, IWorkspaceViewModel
{
	private bool isHidden;
	protected readonly EventAggregator EventAggregator = ea;

	#region Inherited from IToolViewModel (derived from IViewModelBase, IViewModelExtData)

	public abstract string Header { get; set; }
	public abstract string Name { get; set; }
	public abstract string WinTitle { get; set; }
	public bool IsHidden
	{ 
		get => isHidden;
		set 
		{
			if (isHidden != value)
			{
				isHidden = value;
				ChangedIsHidden?.Invoke(this, isHidden);
			}; 
		}
	}
	public EventHandler<bool>? ChangedIsHidden { get; set; }
	public abstract DockingPosition DockPosition { get; set; }
	public abstract bool IsDocument { get; }
	public Dictionary<string, object> CustomTags { get; set; } = new();

	public virtual void GotFocus(object sender, RoutedEventArgs e)
	{
	}

	public virtual void OnIsVisibleChanged(bool isVisible)
	{
	}

	public virtual void Clean()
	{
	}
	
	public virtual Task CleanAsync(CancellationToken ct = new CancellationToken())
	{
		return Task.CompletedTask;
	}

	#endregion
}