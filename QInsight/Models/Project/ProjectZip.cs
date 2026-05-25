using System.IO;
using System.Text;
using System.Xml.Serialization;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;

namespace Qenex.QInsight.Models.Project;

public class ProjectZip
{
    private const string ModuleEntryName = "XmlModule.xml";
    private const string WorkspaceLayoutEntryName = "WorkspaceLayout.xml";
    private const string ScriptDocumentsEntryName = "ScriptDocuments.xml";
    private const string WorkspaceFileExtension = ".ws";

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
            
            if (stream.Key == ModuleEntryName)
            {
                var xmlModule = XmlInOut<XmlModule>.LoadFromStream(memoryStream);
                projectData.Module = xmlModule;
            }
            else if (stream.Key.EndsWith(WorkspaceFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                var workspace = LoadWorkspaceProjectData(memoryStream, stream.Key, logger);
                if (workspace != null)
                {
                    projectData.Workspaces.Add(workspace);
                }
            }
            else if (stream.Key == WorkspaceLayoutEntryName)
            {
                projectData.WorkspaceLayout = memoryStream.ToArray();
            }
            else if (stream.Key == ScriptDocumentsEntryName)
            {
                projectData.ScriptDocuments = LoadScriptDocuments(memoryStream, logger);
            }
            else if (stream.Key.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            {
                var reader = new StreamReader(memoryStream);
                var scriptContent = await reader.ReadToEndAsync();
                scriptFiles.Add(stream.Key, scriptContent);
            }
        }

        if (projectData.Module == null)
        {
            return null;
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

    public static async Task ZipProjectFileAsync(
        string zipFilePath,
        RealProjectData realProjectData,
        IEnumerable<WorkspaceProjectData> workspaces,
        IEnumerable<ScriptDocumentProjectData> scriptDocuments,
        Stream? workspaceLayoutStream,
        ILogger? logger = null)
    {
        var prjZip = new Qenex.QSuite.Helpers.ProjectFile.ProjectZip(logger);
        var xmlModuleHandler = new XmlModuleHandler([], [], logger);
        var xmlModule = xmlModuleHandler.CreateXmlModule(realProjectData.Module);
        var streams = CreateProjectStreams(xmlModule, workspaces, scriptDocuments, workspaceLayoutStream);

        await prjZip.ZipAsync(zipFilePath, streams);
    }

    #endregion

    private static Dictionary<string, Stream> CreateProjectStreams(
        XmlModule xmlModule,
        IEnumerable<WorkspaceProjectData> workspaces,
        IEnumerable<ScriptDocumentProjectData> scriptDocuments,
        Stream? workspaceLayoutStream)
    {
        var streams = new Dictionary<string, Stream>
        {
            { ModuleEntryName, CreateXmlModuleStream(xmlModule) }
        };

        foreach (var script in xmlModule.Scripts.Where(s => s.FileName.EndsWith(".py", StringComparison.OrdinalIgnoreCase)))
        {
            streams[script.FileName] = new MemoryStream(Encoding.UTF8.GetBytes(script.Content ?? string.Empty));
        }

        foreach (var workspace in workspaces)
        {
            streams[$"{workspace.Name}{WorkspaceFileExtension}"] = CreateWorkspaceStream(workspace);
        }

        streams[ScriptDocumentsEntryName] = CreateScriptDocumentsStream(scriptDocuments);

        if (workspaceLayoutStream != null)
        {
            workspaceLayoutStream.Position = 0;
            var layoutStream = new MemoryStream();
            workspaceLayoutStream.CopyTo(layoutStream);
            layoutStream.Position = 0;
            streams[WorkspaceLayoutEntryName] = layoutStream;
        }

        return streams;
    }

    private static Stream CreateXmlModuleStream(XmlModule xmlModule)
    {
        var xmlModuleForProjectFile = new XmlModule
        {
            Name = xmlModule.Name,
            Label = xmlModule.Label,
            Description = xmlModule.Description,
            Version = xmlModule.Version,
            Author = xmlModule.Author,
            Company = xmlModule.Company,
            CreatedOn = xmlModule.CreatedOn,
            DriverReferences = xmlModule.DriverReferences,
            Drivers = xmlModule.Drivers,
            Protocols = xmlModule.Protocols,
            Presentations = xmlModule.Presentations,
            Conversions = xmlModule.Conversions,
            Events = xmlModule.Events,
            Variables = xmlModule.Variables,
            Scripts = xmlModule.Scripts.Select(script => new XmlScript
            {
                FileName = script.FileName,
                ExecutionMode = script.ExecutionMode,
                AdditionalInfo = script.AdditionalInfo
            }).ToList()
        };

        var stream = new MemoryStream();
        XmlInOut<XmlModule>.SaveToStream(stream, xmlModuleForProjectFile);
        stream.Position = 0;
        return stream;
    }

    private static Stream CreateWorkspaceStream(WorkspaceProjectData workspace)
    {
        var stream = new MemoryStream();
        var serializer = new XmlSerializer(typeof(WorkspaceProjectData));
        serializer.Serialize(stream, workspace);
        stream.Position = 0;
        return stream;
    }

    private static Stream CreateScriptDocumentsStream(IEnumerable<ScriptDocumentProjectData> scriptDocuments)
    {
        var stream = new MemoryStream();
        var serializer = new XmlSerializer(typeof(ScriptDocumentProjectDataCollection));
        var collection = new ScriptDocumentProjectDataCollection
        {
            Documents = scriptDocuments.ToList()
        };

        serializer.Serialize(stream, collection);
        stream.Position = 0;
        return stream;
    }

    private static WorkspaceProjectData? LoadWorkspaceProjectData(Stream stream, string entryName, ILogger? logger)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(WorkspaceProjectData));
            return serializer.Deserialize(stream) as WorkspaceProjectData;
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Warn, $"Workspace file \"{entryName}\" could not be loaded.", e);
            return null;
        }
    }

    private static List<ScriptDocumentProjectData> LoadScriptDocuments(Stream stream, ILogger? logger)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(ScriptDocumentProjectDataCollection));
            var collection = serializer.Deserialize(stream) as ScriptDocumentProjectDataCollection;
            return collection?.Documents ?? [];
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Warn, "Script documents file could not be loaded.", e);
            return [];
        }
    }
}
