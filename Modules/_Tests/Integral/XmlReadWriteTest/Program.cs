using Qenex.QLibs.XmlInOut;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;

namespace Qenex.QSuite.Modules.Tests.XmlReadWriteTest;

class Program
{
    static void Main(string[] args)
    {
        
        try
        {
            // Deserialize settings from file
            var xmlModule = XmlInOut<XmlModule>.LoadFromFile(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\ModuleTest.xml");
            
            // Serialize settings to file
            XmlInOut<XmlModule>.SaveToFile(@"..\..\..\..\..\..\ModuleXmlHandler\Docs\ModuleTest_out.xml", xmlModule);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);

        }
       

    }
}