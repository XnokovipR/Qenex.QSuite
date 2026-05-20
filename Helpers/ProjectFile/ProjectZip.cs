using System.IO.Compression;
using Qenex.QLibs.XmlInOut;
using System.Text;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Modules.Module;

namespace Qenex.QSuite.Helpers.ProjectFile;

public class ProjectZip(ILogger? logger = null)
{
    public Dictionary<string, Stream> Unzip(string zipFilePath)
    {
        var streams = new Dictionary<string, Stream>();
       
        try
        {
            using var zipArchive = ZipFile.OpenRead(zipFilePath);
            foreach (var entry in zipArchive.Entries)
            {
                var entryStream = entry.Open();
                
                var memoryStream = new MemoryStream();
                entryStream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                
                streams.Add(entry.FullName, memoryStream);
            }
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Error, e.Message);
        }
         
        return streams;
    }

    public async Task ZipAsync(string filePath, Dictionary<string, Stream> streams)
    {
        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
            using var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);

			foreach (var stream in streams)
            {
                if (string.IsNullOrEmpty(stream.Key))
                {
                    throw new NullReferenceException("Cannot zip stream with empty key.");
                }

                var zipEntry = zipArchive.CreateEntry(stream.Key, CompressionLevel.Optimal);
                stream.Value.Position = 0;
                await using var zipStream = zipEntry.Open();
                await stream.Value.CopyToAsync(zipStream);
            }
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Error, e.Message);
        }

    }

    public async Task ZipModuleAsync(string filePath, IModuleBase module)
    {
        var xmlModuleHandler = new XmlModuleHandler([], [], logger);
        var xmlModule = xmlModuleHandler.CreateXmlModule(module);
        await ZipModuleAsync(filePath, xmlModule);
    }

    public async Task ZipModuleAsync(string filePath, XmlModule xmlModule)
    {
        var streams = new Dictionary<string, Stream>
        {
            { "XmlModule.xml", CreateXmlModuleStream(xmlModule) }
        };

        foreach (var script in xmlModule.Scripts.Where(s => s.FileName.EndsWith(".py", StringComparison.OrdinalIgnoreCase)))
        {
            streams[script.FileName] = new MemoryStream(Encoding.UTF8.GetBytes(script.Content ?? string.Empty));
        }

        await ZipAsync(filePath, streams);
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
}
