using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.Helpers.ProjectFile;
using Qenex.QSuite.Controls.Control;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;

namespace Qenex.QInsight.Models.Project;

/// <summary>Outcome of opening a project file. Data is set only for Success;
/// PasswordRequired / WrongPasswordOrCorrupt drive the password prompt loop.</summary>
public class ProjectOpenResult
{
    public ProjectUnzipStatus Status { get; init; } = ProjectUnzipStatus.Error;
    public ProjectFilesData? Data { get; init; }
}

public class ProjectZip
{
    private const string ModuleEntryName = "XmlModule.xml";
    private const string WorkspaceLayoutEntryName = "WorkspaceLayout.xml";
    private const string ScriptDocumentsEntryName = "ScriptDocuments.xml";
    private const string WorkspaceFileExtension = ".ws";

    #region Zip / unzip project file

    public static async Task<ProjectOpenResult> UnzipProjectFileAsync(string zipFilePath, string? password = null, ILogger? logger = null)
    {
        var projectData = new ProjectFilesData(logger);

        var prjZip = new Qenex.QSuite.Helpers.ProjectFile.ProjectZip(logger);
        var unzipResult = prjZip.UnzipProject(zipFilePath, password);
        if (unzipResult.Status != ProjectUnzipStatus.Success)
        {
            return new ProjectOpenResult { Status = unzipResult.Status };
        }

        var streams = unzipResult.Streams;

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
                var workspace = LoadWorkspaceProjectData(memoryStream, out var loadFailure);
                if (workspace != null)
                {
                    projectData.Workspaces.Add(workspace);
                }
                else
                {
                    projectData.FailedWorkspaces.Add(loadFailure ?? $"Workspace entry \"{stream.Key}\" could not be loaded.");
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
            return new ProjectOpenResult();
        }

        foreach (var script in projectData.Module.Scripts)
        {
            if (scriptFiles.TryGetValue(script.FileName, out var content))
            {
                script.Content = content;
            }
            else
            {
                projectData.MissingScriptFiles.Add(script.FileName);
            }
        }

        return new ProjectOpenResult { Status = ProjectUnzipStatus.Success, Data = projectData };
    }

    public static async Task ZipProjectFileAsync(
        string zipFilePath,
        RealProjectData realProjectData,
        IEnumerable<WorkspaceProjectData> workspaces,
        IEnumerable<ScriptDocumentProjectData> scriptDocuments,
        Stream? workspaceLayoutStream,
        string? password = null,
        ILogger? logger = null)
    {
        var prjZip = new Qenex.QSuite.Helpers.ProjectFile.ProjectZip(logger);
        realProjectData.Module.Specification.Modified = DateTime.Now;
        var xmlModuleHandler = new XmlModuleHandler([], [], logger);
        var xmlModule = xmlModuleHandler.CreateXmlModule(realProjectData.Module);
        var streams = CreateProjectStreams(xmlModule, workspaces, scriptDocuments, workspaceLayoutStream);

        await prjZip.ZipProjectAsync(zipFilePath, streams, password);
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
            FormatVersion = xmlModule.FormatVersion,
            Name = xmlModule.Name,
            Label = xmlModule.Label,
            Description = xmlModule.Description,
            Version = xmlModule.Version,
            Author = xmlModule.Author,
            Company = xmlModule.Company,
            CreationDate = xmlModule.CreationDate,
            Modified = xmlModule.Modified,
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
                Blocking = script.Blocking,
                TimeoutMs = script.TimeoutMs,
                AdditionalInfo = script.AdditionalInfo,
                IsEnabled = script.IsEnabled,
                IsReplayEnabled = script.IsReplayEnabled
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
        var serializer = new DataContractSerializer(typeof(WorkspaceProjectData), GetKnownControlTypes());
        serializer.WriteObject(stream, workspace);
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

    private static WorkspaceProjectData? LoadWorkspaceProjectData(MemoryStream stream, out string? loadFailure)
    {
        try
        {
            var serializer = new DataContractSerializer(typeof(WorkspaceProjectData), GetKnownControlTypes());
            loadFailure = null;
            return serializer.ReadObject(stream) as WorkspaceProjectData;
        }
        catch (Exception e)
        {
            loadFailure = DescribeWorkspaceLoadFailure(stream, e);
            return null;
        }
    }

    // The serializer exception ("...data contract that is not expected...") does not tell
    // the user what is actually wrong. Read the raw XML instead and name the real cause:
    // the workspace by its window title and the control types no loaded assembly provides.
    private static string DescribeWorkspaceLoadFailure(MemoryStream stream, Exception exception)
    {
        try
        {
            stream.Position = 0;
            var document = System.Xml.Linq.XDocument.Load(stream);
            System.Xml.Linq.XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

            var title = document.Root?.Elements()
                .FirstOrDefault(element => element.Name.LocalName == "WinTitle")?.Value;
            var workspaceLabel = string.IsNullOrWhiteSpace(title) ? "workspace" : $"workspace \"{title}\"";

            var knownTypeNames = GetKnownControlTypes()
                .Select(type => type.Name)
                .ToHashSet(StringComparer.Ordinal);
            var missingControls = document.Descendants()
                .Where(element => element.Name.LocalName == "ControlBase")
                .Select(element => element.Attribute(xsi + "type")?.Value)
                .OfType<string>()
                .Select(typeReference => typeReference[(typeReference.IndexOf(':') + 1)..])
                .Distinct()
                .Where(typeName => !knownTypeNames.Contains(typeName))
                .Select(typeName => typeName.EndsWith("ViewModel", StringComparison.Ordinal)
                    ? typeName[..^"ViewModel".Length]
                    : typeName)
                .ToList();

            if (missingControls.Count > 0)
            {
                var controlList = string.Join(", ", missingControls.Select(name => $"\"{name}\""));
                return $"Control {controlList} is not available in this installation "
                       + $"(control assembly missing or failed to load) - {workspaceLabel} was skipped.";
            }

            return $"The {workspaceLabel} could not be loaded: {exception.Message}";
        }
        catch
        {
            return $"The workspace could not be loaded: {exception.Message}";
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

    // Known-types pro DataContract serializaci workspace. Controls se nacitaji jako pluginy,
    // proto je zjistujeme dynamicky z nactenych control assembly (vsechny ControlBase potomky)
    // misto pevneho seznamu typeof(...). Plugin assembly jsou nactene jiz pri startu (toolbox).
    // Scanujeme JEN Qenex control assembly - ne cely AppDomain (jine assembly, napr. s chybejici
    // zavislosti Microsoft.Web.WebView2.Core, by pri GetTypes() hazely vyjimku).
    private static IEnumerable<Type> GetKnownControlTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith("Qenex.QSuite.Controls", StringComparison.Ordinal) == true)
            .SelectMany(GetLoadableTypes)
            .Where(type => typeof(ControlBase).IsAssignableFrom(type)
                           && type is { IsClass: true, IsAbstract: false })
            .ToList();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null)!;
        }
        catch
        {
            return [];
        }
    }

}
