using Qenex.QLibs.QUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qenex.QInsight.ViewModels;

public class ShellWindowModel : PropertyChangedBaseWithValidation
{
	#region Private

	private readonly EventAggregator eventAggregator;
	
	// ViewModels
	private SolutionExplorerViewModel solutionExplorerViewModel;
	private LogsViewModel logsViewModel;
	private PropertiesViewModel propertiesViewModel;
	private ControlsViewModel controlsViewModel;
	private ObservableCollection<ViewModelBase> viewModels;

	#endregion
	
	#region Ctors

	public ShellWindowModel()
	{
		eventAggregator = new EventAggregator();
		ViewModels = new ObservableCollection<ViewModelBase>();
		
		CreateViewModels();
	}

	#endregion

	#region Properties

	public LogsViewModel LogsViewModel
	{
		get => logsViewModel;
		set { logsViewModel = value; OnPropertyChanged(); }
	}

	public ObservableCollection<ViewModelBase> ViewModels
	{
		get => viewModels;
		set { viewModels = value; OnPropertyChanged(); }
	}

	#endregion

	#region Privates

	private void CreateViewModels()
	{
		solutionExplorerViewModel = new SolutionExplorerViewModel(eventAggregator);
		ViewModels.Add(solutionExplorerViewModel);

		controlsViewModel = new ControlsViewModel(eventAggregator);
		ViewModels.Add(controlsViewModel);

		LogsViewModel = new LogsViewModel(eventAggregator);
		//logger.RegisterSubscriber(logViewModel);
		ViewModels.Add(LogsViewModel);

		propertiesViewModel = new PropertiesViewModel(eventAggregator);
		ViewModels.Add(propertiesViewModel);
	}

	#endregion
}
