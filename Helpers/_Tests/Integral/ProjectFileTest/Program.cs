using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Helpers.ProjectFile;

namespace Qenex.QSuite.Helpers.Tests.ProjectFileTest;

class Program
{
    static async Task  Main(string[] args)
    {

        await ZipTestAsync();
        var xmlModule = await UnzipTestAsync();
    }


    private static async Task<XmlModule?> UnzipTestAsync()
    {
        var prjZip = new ProjectZip();
        var streams = prjZip.Unzip("ModuleTestProject.zip");

        foreach (var stream in streams)
        {       
            if (stream.Key == "XmlModule.xml")
            {
                using var memoryStream = new MemoryStream();
                await stream.Value.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

				var xmlModule = XmlInOut<XmlModule>.LoadFromStream(memoryStream);
                return xmlModule;
            }
        }

        return null;
    }
    
    
    
    private static async Task ZipTestAsync()
    {
        var xmlModule = XmlInOut<XmlModule>.LoadFromFile(@"..\..\..\..\..\..\..\Modules\ModuleXmlHandler\Docs\ModuleTest.xml");

        using var stream = new MemoryStream();
        stream.Position = 0;
        XmlInOut<XmlModule>.SaveToStream(stream, xmlModule);
        var fileDict = new Dictionary<string, Stream>()
        {
            { "XmlModule.xml", stream }
        };
        
        var prjZip = new ProjectZip();
        await prjZip.ZipAsync("ModuleTestProject.zip", fileDict);
    }
}