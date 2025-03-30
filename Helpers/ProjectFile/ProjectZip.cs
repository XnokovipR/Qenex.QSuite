using System.IO.Compression;
using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;

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
            using var zipArchive = ZipFile.Open(filePath, ZipArchiveMode.Create);

			foreach (var stream in streams)
            {
                if (string.IsNullOrEmpty(stream.Key) || stream.Value.Length == 0)
                {
                    throw new NullReferenceException($"Cannot zip \"{stream.Key}\" stream is empty or key is empty");
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
}