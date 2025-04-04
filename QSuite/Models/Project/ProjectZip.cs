using System.IO;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;

namespace Qenex.QSuite.Models.Project;

public class ProjectZip
{
    #region Zip / unzip project file

    public static async Task<ProjectData?> UnzipProjectFileAsync(string zipFilePath, ILogger? logger = null)
    {
        var projectData = new ProjectData(logger);

        var prjZip = new Helpers.ProjectFile.ProjectZip();
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
        if (isAllPropertiesLoaded) 
        {
            logger?.Log(LogLevel.Info, $"Project file \"{Path.GetFileName(zipFilePath)}\" loaded successfully.");
            return projectData;
        }
        else
        {
            logger?.Log(LogLevel.Warn, $"Failed to load project file \"{Path.GetFileName(zipFilePath)}\".");
        }

        return null;

    }

    #endregion
}