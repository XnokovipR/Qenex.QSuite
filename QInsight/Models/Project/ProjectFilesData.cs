using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;

namespace Qenex.QInsight.Models.Project;

public class ProjectFilesData
{
    private ILogger? logger { get; set; }

    #region Constructors

    public ProjectFilesData(ILogger? logger = null)
    {
        this.logger = logger;
    }

    #endregion


    #region Project file properties

    public XmlModule Module { get; set; } = null!;
    public List<WorkspaceProjectData> Workspaces { get; set; } = [];
    public List<ScriptDocumentProjectData> ScriptDocuments { get; set; } = [];
    public byte[]? WorkspaceLayout { get; set; }
    // Script file names listed in XmlModule.xml whose .py entry was not found in the
    // project zip; the validation alert reports them (content cannot tell — empty is legal).
    public List<string> MissingScriptFiles { get; set; } = [];
    // User-readable descriptions of workspaces that failed to load (which control is
    // missing, which workspace was skipped). The validation alert shows them and the
    // open flow folds them into its single closing log line instead of a clean "opened".
    public List<string> FailedWorkspaces { get; set; } = [];

    #endregion
}
