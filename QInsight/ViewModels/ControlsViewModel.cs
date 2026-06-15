using System.Collections.ObjectModel;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.Controls.Control;
using Telerik.Windows.Controls;
using System.Windows.Media;
using Qenex.QInsight.Views;

namespace Qenex.QInsight.ViewModels;

public class ControlsViewModel : ViewModelBase
{
    #region  Fields

    private bool isViewLoaded;
    private List<PluginDetails> controlPlugins = [];
    private readonly PluginLoader pluginLoader = new();

    #endregion

    #region Constructors

    public ControlsViewModel(EventAggregator ea) : base(ea)
    {
        isViewLoaded = false;
        BackgroundColor = ShellWindow.TextEditorBackgroubndColor;
        Controls = new ObservableCollection<IControlBase>();
        UserControlLoadedCommand = new RelayCommand<RadDocking>(OnUserControlLoaded);
    }

    #endregion

    #region Properties

    public int LastControlId { get; set; } = 0;

    public ObservableCollection<IControlBase> Controls { get; set; }
    public RelayCommand<RadDocking> UserControlLoadedCommand { get; set; }
    public Color BackgroundColor { get; }

    #endregion

    #region ViewModelBase implementation

    public override string Header { get; set; } = "Controls";
    public override string Name { get; set; } = "ControlsViewModel";
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Left;
    public override bool IsDocument => false;

    #endregion

    #region Public methods

    /// <summary>
    /// Predani control pluginu nactenych ze slozky .\Controls. Volano po nacteni pluginu
    /// v ShellWindowModel; toolbox se naplni jakmile jsou k dispozici data i nactena View.
    /// </summary>
    public void SetControlPlugins(IEnumerable<PluginDetails> plugins)
    {
        controlPlugins = plugins.ToList();
        PopulateControls();
    }

    #endregion

    #region Commands methods

    private void OnUserControlLoaded(RadDocking radDocking)
    {
        isViewLoaded = true;
        PopulateControls();
    }

    #endregion

    #region Private methods

    private void PopulateControls()
    {
        if (!isViewLoaded) return;

        Controls.Clear();

        // Vsechny controls (Graph, Signal, ...) se nacitaji dynamicky jako plugin ze slozky .\Controls.
        foreach (var plugin in controlPlugins)
        {
            if (pluginLoader.LoadPlugin<IControlBase>(plugin.PathName) is { } instance)
            {
                Controls.Add(instance);
            }
        }
    }

    #endregion
}
