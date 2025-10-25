using Qenex.QLibs.QUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.Models.Project;
using Qenex.QInsight.Views;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.LogSystems.LogSystem;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public partial class ShellWindowModel : PropertyChangedBaseWithValidation
{
	#region Private

	private readonly EventAggregator eventAggregator;
	private readonly Logger logger;
	
	// Plugins
	private PluginLoader pluginLoader;
	private List<PluginDetails> driverPlugins = null!;
	private List<PluginDetails> protocolPlugins = null!;
	
	// Project
	private RealProjectData realProjectData;
	private bool isProjectMade;

	// ViewModels
	private RadDocking shellRadDocking;
	private SolutionExplorerViewModel solutionExplorerViewModel;
	private LogsViewModel logsViewModel;
	private PropertiesViewModel propertiesViewModel;
	private ControlsViewModel controlsViewModel;
	private ObservableCollection<IViewModelBase> viewModels;

	#endregion

	#region Constructors

	public ShellWindowModel()
	{
		eventAggregator = new EventAggregator();
		logger = new Logger(LogLevel.Trace);
		ViewModels = [];

		CreateViewModels();
		CreateCommands();
		SubscribeEventAggregatorMessages();
	}

	#endregion

	#region Properties

	public LogsViewModel LogsViewModel
	{
		get => logsViewModel;
		set { logsViewModel = value; OnPropertyChanged(); }
	}

	public ObservableCollection<IViewModelBase> ViewModels
	{
		get => viewModels;
		set { viewModels = value; OnPropertyChanged(); }
	}

	#endregion

	#region Private methods

	private void CreateViewModels()
	{
		solutionExplorerViewModel = new SolutionExplorerViewModel(eventAggregator);
		ViewModels.Add(solutionExplorerViewModel);

		controlsViewModel = new ControlsViewModel(eventAggregator);
		ViewModels.Add(controlsViewModel);

		LogsViewModel = new LogsViewModel(eventAggregator);
		logger.RegisterSubscriber(LogsViewModel);
		ViewModels.Add(LogsViewModel);

		propertiesViewModel = new PropertiesViewModel(eventAggregator);
		ViewModels.Add(propertiesViewModel);
	}

	#endregion
}
