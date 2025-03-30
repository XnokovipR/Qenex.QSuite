using System.IO.Compression;

namespace Qenex.QSuite.ProjectFile;

public class ProjectExtractor
{
    public static Dictionary<string, Stream> ExtractProject(string zipFilePath)
    {
       var streams = new Dictionary<string, Stream>();
       
       using var zipArchive = ZipFile.OpenRead(zipFilePath);
         foreach (var entry in zipArchive.Entries)
         {
              var stream = entry.Open();
              streams.Add(entry.FullName, stream);
         }
         
         return streams;
    }
}