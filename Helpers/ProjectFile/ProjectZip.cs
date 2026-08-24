using System.IO.Compression;
using Qenex.QLibs.XmlInOut;
using System.Text;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Modules.Module;

namespace Qenex.QSuite.Helpers.ProjectFile;

public enum ProjectUnzipStatus
{
    Success,
    PasswordRequired,
    WrongPasswordOrCorrupt,
    Error
}

/// <summary>Result of opening a .qproj file: entry streams keyed by entry name, or a
/// status telling the caller why there are none (password flow, unreadable file).</summary>
public class ProjectUnzipResult
{
    public ProjectUnzipStatus Status { get; init; } = ProjectUnzipStatus.Error;
    public Dictionary<string, Stream> Streams { get; init; } = [];
}

public class ProjectZip(ILogger? logger = null)
{
    public Dictionary<string, Stream> Unzip(string zipFilePath)
    {
        var streams = new Dictionary<string, Stream>();

        try
        {
            using var zipArchive = ZipFile.OpenRead(zipFilePath);
            ReadZipEntries(zipArchive, streams);
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Error, e.Message);
        }

        return streams;
    }

    /// <summary>
    /// Opens a .qproj file: an encrypted container (built-in key or password) or, for
    /// projects saved before encryption existed, a plain zip. Pass the user password
    /// when the previous attempt returned PasswordRequired or WrongPasswordOrCorrupt.
    /// </summary>
    public ProjectUnzipResult UnzipProject(string filePath, string? password = null)
    {
        try
        {
            var fileBytes = File.ReadAllBytes(filePath);

            byte[] zipBytes;
            switch (QprojContainer.DetectKind(fileBytes))
            {
                case QprojFileKind.Container:
                    var status = QprojContainer.TryDecrypt(fileBytes, password, out var plainZip);
                    switch (status)
                    {
                        case QprojDecryptStatus.Success:
                            zipBytes = plainZip!;
                            break;
                        case QprojDecryptStatus.PasswordRequired:
                            return new ProjectUnzipResult { Status = ProjectUnzipStatus.PasswordRequired };
                        case QprojDecryptStatus.WrongPasswordOrCorrupt:
                            return new ProjectUnzipResult { Status = ProjectUnzipStatus.WrongPasswordOrCorrupt };
                        default:
                            logger?.Log(LogLevel.Error, $"Project file \"{Path.GetFileName(filePath)}\" is damaged and cannot be opened.");
                            return new ProjectUnzipResult();
                    }
                    break;

                case QprojFileKind.LegacyZip:
                    zipBytes = fileBytes;
                    break;

                default:
                    logger?.Log(LogLevel.Error, $"File \"{Path.GetFileName(filePath)}\" is not a QInsight project file.");
                    return new ProjectUnzipResult();
            }

            var streams = new Dictionary<string, Stream>();
            using var zipArchive = new ZipArchive(new MemoryStream(zipBytes), ZipArchiveMode.Read);
            ReadZipEntries(zipArchive, streams);
            return new ProjectUnzipResult { Status = ProjectUnzipStatus.Success, Streams = streams };
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Error, e.Message);
            return new ProjectUnzipResult();
        }
    }

    public async Task ZipAsync(string filePath, Dictionary<string, Stream> streams)
    {
        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
            await WriteZipArchiveAsync(fileStream, streams);
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Error, e.Message);
        }

    }

    /// <summary>
    /// Saves a .qproj as an encrypted container; a null password means the built-in
    /// key. Unlike ZipAsync this throws on failure so the caller's backup/restore
    /// logic can react.
    /// </summary>
    public async Task ZipProjectAsync(string filePath, Dictionary<string, Stream> streams, string? password = null)
    {
        using var zipStream = new MemoryStream();
        await WriteZipArchiveAsync(zipStream, streams);
        var container = QprojContainer.Encrypt(zipStream.ToArray(), password);
        await File.WriteAllBytesAsync(filePath, container);
    }

    private static void ReadZipEntries(ZipArchive zipArchive, Dictionary<string, Stream> streams)
    {
        foreach (var entry in zipArchive.Entries)
        {
            var entryStream = entry.Open();

            var memoryStream = new MemoryStream();
            entryStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            streams.Add(entry.FullName, memoryStream);
        }
    }

    private static async Task WriteZipArchiveAsync(Stream target, Dictionary<string, Stream> streams)
    {
        using var zipArchive = new ZipArchive(target, ZipArchiveMode.Create, leaveOpen: true);

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

    public async Task ZipModuleAsync(string filePath, IModuleBase module)
    {
        module.Specification.Modified = DateTime.Now;
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
}
