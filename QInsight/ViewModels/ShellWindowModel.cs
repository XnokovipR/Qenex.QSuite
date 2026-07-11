using Qenex.QLibs.QUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Qenex.QInsight.AppConfig;
using Qenex.QInsight.Licensing;
using Qenex.QInsight.Models.Project;
using Qenex.QInsight.Views;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Scripting.ScriptingEngine;
using Telerik.Windows.Controls;

namespace Qenex.QInsight.ViewModels;

public partial class ShellWindowModel : PropertyChangedBaseWithValidation
{
	#region Private

	private const string AppTitle = "QInsight";
	
	private string runtimeSettingLayoutFile = "QInsightRuntimeSettings.xml";
	private string editModeSettingLayoutFile = "QInsightEditSettings.xml";

	private readonly EventAggregator eventAggregator;
	private readonly Logger logger;
	private readonly LicenseService licenseService;
	
	// Plugins
	private PluginLoader pluginLoader;
	private List<PluginDetails> driverPlugins = null!;
	private List<PluginDetails> protocolPlugins = null!;
	private List<PluginDetails> controlPlugins = null!;

	// Project
	private RealProjectData realProjectData;
	private bool isProjectMade;
	private bool isEditProjectEnabled;
	private bool isEditVariableEnabled;
	private bool isEditConversionEnabled;
	private bool isEditPresentationEnabled;
	private bool isEditEventEnabled;
	private string? currentProjectFilePath;
	private ScriptingContext? pythonInterpreterStandaloneContext;

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
		licenseService = new LicenseService(logger);
		ViewModels = [];

		CreateViewModels();
		CreateCommands();
		SubscribeEventAggregatorMessages();

		licenseService.StateChanged += (_, _) =>
		{
			NotifyLicenseDependentCommands();
			UpdateLicenseBadge();
		};
		licenseService.Initialize();
		UpdateLicenseBadge();
	}

	#endregion

	#region Properties

	public bool IsRuntimeStarted { get; set; } = false;

	public bool IsReplayControlEnabled
	{
		get;
		set { field = value; OnPropertyChanged(); }
	}

	public bool IsReplaySeekEnabled
	{
		get;
		set { field = value; OnPropertyChanged(); }
	}

	public double ReplayPositionSeconds
	{
		get;
		set
		{
			if (Math.Abs(field - value) < 0.0005)
			{
				return;
			}

			field = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(ReplayCurrentTimeText));
			SetReplayPositionText(field);

			if (!isUpdatingReplayPositionFromDriver && IsReplaySeekEnabled)
			{
				var seekRequestVersion = ++replaySeekRequestVersion;
				_ = SeekReplayPositionAsync(value, seekRequestVersion);
			}
		}
	}

	public string ReplayPositionText
	{
		get;
		set
		{
			if (field == value)
			{
				return;
			}

			field = value;
			OnPropertyChanged();

			if (isUpdatingReplayPositionFromDriver
			    || isUpdatingReplayPositionTextFromPosition
			    || !IsReplaySeekEnabled)
			{
				return;
			}

			if (!TryParseReplayTime(value, out var replayPosition))
			{
				SetReplayPositionText(ReplayPositionSeconds);
				return;
			}

			var replayPositionSeconds = Math.Clamp(
				replayPosition.TotalSeconds,
				0,
				ReplayDurationSeconds);
			if (Math.Abs(ReplayPositionSeconds - replayPositionSeconds) < 0.0005)
			{
				SetReplayPositionText(replayPositionSeconds);
				return;
			}

			ReplayPositionSeconds = replayPositionSeconds;
		}
	} = FormatReplayTime(TimeSpan.Zero);

	public double ReplayDurationSeconds
	{
		get;
		set
		{
			if (Math.Abs(field - value) < 0.001)
			{
				return;
			}

			field = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(ReplayDurationText));
		}
	}

	public string ReplayCurrentTimeText => FormatReplayTime(TimeSpan.FromSeconds(ReplayPositionSeconds));

	public string ReplayDurationText => FormatReplayTime(TimeSpan.FromSeconds(ReplayDurationSeconds));

	public string ReplayPauseResumeText
	{
		get;
		set { field = value; OnPropertyChanged(); }
	} = "Pause";

	public string WindowTitle
	{
		get;
		set { field = value; OnPropertyChanged(); }
	} = AppTitle;

	/// <summary>Shows the "FREE NON-COMMERCIAL LICENCE" badge in the top-right corner
	/// of the main window while a Free-tier license is active.</summary>
	public bool IsFreeLicense
	{
		get;
		set { field = value; OnPropertyChanged(); }
	}

	/// <summary>Badge text; with a project open it includes the communicated-signal
	/// counter (e.g. "FREE NON-COMMERCIAL LICENCE · 12/10 signals").</summary>
	public string LicenseBadgeText
	{
		get;
		set { field = value; OnPropertyChanged(); }
	} = FreeLicenseBadgeCaption;

	/// <summary>True when the communicated-signal count exceeds the Free limit;
	/// the badge turns red so the disabled Connect/Replay buttons explain themselves.</summary>
	public bool IsLicenseBadgeAlert
	{
		get;
		set { field = value; OnPropertyChanged(); }
	}

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

	private const string FreeLicenseBadgeCaption = "FREE NON-COMMERCIAL LICENCE";

	private void UpdateLicenseBadge()
	{
		IsFreeLicense = licenseService.Status == LicenseStatus.Valid && licenseService.IsFreeTier;

		if (!IsFreeLicense
			|| CommunicatedSignals.GetLimit(licenseService) is not { } signalLimit
			|| !isProjectMade
			|| realProjectData?.Module == null)
		{
			LicenseBadgeText = FreeLicenseBadgeCaption;
			IsLicenseBadgeAlert = false;
			return;
		}

		var communicatedCount = CommunicatedSignals.Count(realProjectData.Module);
		LicenseBadgeText = $"{FreeLicenseBadgeCaption} · {communicatedCount}/{signalLimit} signals";
		IsLicenseBadgeAlert = communicatedCount > signalLimit;
	}

	private void SetProjectWindowTitle(string? projectFilePath)
	{
		if (string.IsNullOrWhiteSpace(projectFilePath))
		{
			WindowTitle = AppTitle;
			return;
		}

		WindowTitle = $"{AppTitle} - {Path.GetFileNameWithoutExtension(projectFilePath)}";
	}

	#endregion
}
