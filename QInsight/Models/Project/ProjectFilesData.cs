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

    #endregion
}
