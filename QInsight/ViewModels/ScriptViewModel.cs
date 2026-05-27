using System.ComponentModel;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
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
        
        if (ScriptWrapper is INotifyPropertyChanged npc)
        {
	        npc.PropertyChanged += (_, e) =>
	        {
		        if (e.PropertyName == nameof(ScriptWrapper.FileName))
		        {
			        WinTitle = ScriptWrapper.FileName;
		        }
	        };
        }
    }

    #endregion

    #region Properties
    
    public ScriptWrapper ScriptWrapper;
    
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
