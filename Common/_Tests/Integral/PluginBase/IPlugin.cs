namespace Qenex.QSuite.Common.Tests.PluginBase;

public interface IPlugin
{
    string Name { get; set; }
    string Version { get; set; }
    
    void DisplayInfo();
}