using System.ComponentModel;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Qenex.QInsight.EventAggregatorMsgs;
using Qenex.QInsight.Models;
using Qenex.QInsight.ViewModels.ModelWrappers;
using Qenex.QInsight.Views;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripting.Script;

namespace Qenex.QInsight.ViewModels;

public class ScriptViewModel : WorkspaceViewModelBase
{
    #region Constructors

    public ScriptViewModel(EventAggregator ea, ScriptWrapper s) : base(ea)
    {
        ForegroundColor = ShellWindow.ForegroundColor;
        BackgroundColor = ShellWindow.TextEditorBackgroubndColor;
        FontSize = ShellWindow.MainAppSettings.Design.FontSize + 1;
        PyHighlighting = SyntaxHighlighting.LoadPythonHighlighting(ShellWindow.IsDarkTheme);
        
        ScriptWrapper = s;
        Document = new TextDocument(ScriptWrapper.Content);
        Document.TextChanged += (_, _) => ScriptWrapper.Content = Document.Text;

        WinTitle = ScriptWrapper.FileName;
        RunScriptCommand = new RelayCommand<object>(
            _ => EventAggregator.Publish(new RunManualScriptMsg { Script = ScriptWrapper }),
            _ => IsRuntimeRunning && ScriptWrapper.ExecutionMode == ScriptExecutionMode.Manual);
        StopScriptCommand = new RelayCommand<object>(
            _ => EventAggregator.Publish(new StopManualScriptMsg { Script = ScriptWrapper }),
            _ => IsRuntimeRunning && ScriptWrapper.ExecutionMode == ScriptExecutionMode.Manual);

        if (ScriptWrapper is INotifyPropertyChanged npc)
        {
	        npc.PropertyChanged += (_, e) =>
	        {
		        if (e.PropertyName == nameof(ScriptWrapper.FileName))
		        {
			        WinTitle = ScriptWrapper.FileName;
		        }

		        if (e.PropertyName == nameof(ScriptWrapper.ExecutionMode))
		        {
			        RunScriptCommand.OnCanExecuteChanged();
			        StopScriptCommand.OnCanExecuteChanged();
		        }
	        };
        }
    }

    #endregion

    #region Properties
    
    public ScriptWrapper ScriptWrapper;

    public bool IsRuntimeRunning
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            RunScriptCommand.OnCanExecuteChanged();
            StopScriptCommand.OnCanExecuteChanged();
        }
    }

    public RelayCommand<object> RunScriptCommand { get; }
    public RelayCommand<object> StopScriptCommand { get; }

    public IHighlightingDefinition PyHighlighting { get; }
    public Color ForegroundColor { get;  }
    public Color BackgroundColor { get; }
    public int FontSize { get; }
    public TextDocument Document { get; }
    

    #endregion
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "";
    public override string Name { get; set; } = $"WorkspaceViewModel_Script_{Guid.NewGuid().ToString().Replace("-", "_")}";
    public override string WinTitle { get => field; set { field = value; OnPropertyChanged(); } }
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Workspace;
    public override bool IsDocument => true;


	#endregion

	#region Overrides
	#endregion
}
