using Qenex.QLibs.QUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ICSharpCode.AvalonEdit.Highlighting;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.Models;
using Qenex.QInsight.Models.Project;
using Qenex.QInsight.Views;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.LogSystems.LogSystem;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public partial class ShellWindowModel : PropertyChangedBaseWithValidation
{
	#region Private

	internal static readonly IHighlightingDefinition PythonHighlighting  = SyntaxHighlighting.LoadPythonHighlighting(ShellWindow.IsDarkTheme);
	
	private string runtimeSettingLayoutFile = "QInsightRuntimeSettings.xml";
	private string editModeSettingLayoutFile = "QInsightEditSettings.xml";

	private readonly EventAggregator eventAggregator;
	private readonly Logger logger;
	
	// Plugins
	private PluginLoader pluginLoader;
	private List<PluginDetails> driverPlugins = null!;
	private List<PluginDetails> protocolPlugins = null!;
	
	// Project
	private RealProjectData realProjectData;
	private bool isProjectMade;
	private string? currentProjectFilePath;

	// ViewModels
	private RadDocking shellRadDocking;
	private SolutionExplorerViewModel solutionExplorerViewModel;
	private PropertiesViewModel propertiesViewModel;
	private ControlsViewModel controlsViewModel;

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

	public bool IsRuntimeStarted { get; set; } = false;

	public LogsViewModel LogsViewModel
	{
		get;
		set { field = value; OnPropertyChanged(); }
	}
	
	public ScriptLogsViewModel ScriptLogsViewModel
	{
		get;
		set { field = value; OnPropertyChanged(); }
	}

	public ObservableCollection<IViewModelBase> ViewModels
	{
		get;
		set { field = value; OnPropertyChanged(); }
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
		
		ScriptLogsViewModel = new ScriptLogsViewModel(eventAggregator);
		logger.RegisterSubscriber(ScriptLogsViewModel);
		ViewModels.Add(ScriptLogsViewModel);

		propertiesViewModel = new PropertiesViewModel(eventAggregator);
		SetDefaultPropertiesView();
		ViewModels.Add(propertiesViewModel);
	}

	private void SetDefaultPropertiesView()
	{
		propertiesViewModel.SelectedViewModel = new EmptyPropertiesViewModel(eventAggregator);
	}

	#endregion
}
