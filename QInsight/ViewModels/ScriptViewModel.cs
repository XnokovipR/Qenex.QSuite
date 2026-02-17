using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Qenex.QInsight.Views;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripts.Script;

namespace Qenex.QInsight.ViewModels;

public class ScriptViewModel : WorkspaceViewModelBase
{

    private IScriptBase script;
    
    #region Constructors

    public ScriptViewModel(EventAggregator ea, IScriptBase s) : base(ea)
    {
        ForegroundColor = ShellWindow.ForegroundColor;
        BackgroundColor = ShellWindow.BackgroundColor;
        FontSize = ShellWindow.MainAppSettings.Design.FontSize + 1;
        PyHighlighting = ShellWindowModel.PythonHighlighting;
        
        script = s;
        Document = new TextDocument(script.Content);
        Document.TextChanged += (_, _) => script.Content = Document.Text;

        WinTitle = script.FileName;
    }

    #endregion

    #region Properties
    
    public IHighlightingDefinition PyHighlighting { get; }
    public Color ForegroundColor { get;  }
    public Color BackgroundColor { get; }
    public int FontSize { get; }
    public TextDocument Document { get; }

    public string FileName { get => field; set { field = value; script.FileName = value; OnPropertyChanged(); } }
    public string Content { get => field; set { field = value; script.Content = value; OnPropertyChanged(); } }

    #endregion
    
    #region ViewModelBase implementation

    public override string Header { get; set; } = "";
    public override string Name { get; set; } = $"WorkspaceViewModel__{Guid.NewGuid().ToString().Replace("-", "_")}";
    public override string WinTitle { get; set; }
    public override DockingPosition DockPosition { get; set; } = DockingPosition.Workspace;
    public override bool IsDocument => true;
    

    #endregion
}