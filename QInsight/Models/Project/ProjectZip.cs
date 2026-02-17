using System.IO;
using Qenex.QInsight.Models.Project;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;

namespace Qenex.QInsight.Models.Project;

public class ProjectZip
{
    #region Zip / unzip project file

    public static async Task<ProjectFilesData?> UnzipProjectFileAsync(string zipFilePath, ILogger? logger = null)
    {
        var projectData = new ProjectFilesData(logger);

        var prjZip = new Qenex.QSuite.Helpers.ProjectFile.ProjectZip();
        var streams = prjZip.Unzip(zipFilePath);


        var scriptFiles = new Dictionary<string, string>();
        
        foreach (var stream in streams)
        {
            using var memoryStream = new MemoryStream();
            await stream.Value.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            if (stream.Key == "XmlModule.xml")
            {
                var xmlModule = XmlInOut<XmlModule>.LoadFromStream(memoryStream);
                projectData.Module = xmlModule;
            }
            else if (stream.Key.Contains(".py"))
            {
                var reader = new StreamReader(memoryStream);
                var scriptContent = await reader.ReadToEndAsync();
                scriptFiles.Add(stream.Key, scriptContent);
            }
        }

        foreach (var script in projectData.Module.Scripts)
        {
            if (scriptFiles.TryGetValue(script.FileName, out var content))
            {
                script.Content = content;
            }
        }
        
        // test all properties of the projectData
        var isAllPropertiesLoaded = projectData?.Module != null;
        if (!isAllPropertiesLoaded) return null;

        return projectData;
    }

    #endregion
}