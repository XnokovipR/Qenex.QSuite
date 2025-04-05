using System.IO;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.Helpers.ProjectFile;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;

namespace Qenex.QSuite.Models.Project;

public class ProjectData
{
    private ILogger? logger { get; set; }

    #region Constructors

    public ProjectData(ILogger? logger = null)
    {
        this.logger = logger;
    }

    #endregion


    #region Project file properties

    public XmlModule Module { get; set; } = null!;

    #endregion
}