using System.IO;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.Helpers.ProjectFile;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.UnifModule;

namespace Qenex.QSuite.Models.Project;

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

    #endregion
}