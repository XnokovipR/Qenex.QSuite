using System.IO;
using Qenex.QInsight.Models.Project;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite;

namespace Qenex.QInsight.Models.Project;

public class ProjectZip
{
    #region Zip / unzip project file

    public static async Task<ProjectFilesData?> UnzipProjectFileAsync(string zipFilePath, ILogger? logger = null)
    {
        var projectData = new ProjectFilesData(logger);

        var prjZip = new Qenex.QSuite.Helpers.ProjectFile.ProjectZip();
        var streams = prjZip.Unzip(zipFilePath);


        foreach (var stream in streams)
        {
            if (stream.Key == "XmlModule.xml")
            {
                using var memoryStream = new MemoryStream();
                await stream.Value.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var xmlModule = XmlInOut<XmlModule>.LoadFromStream(memoryStream);
                projectData.Module = xmlModule;
            }
        }
        
        // test all properties of the projectData
        var isAllPropertiesLoaded = projectData?.Module != null;
        if (!isAllPropertiesLoaded) return null;

        return projectData;
    }

    #endregion
}