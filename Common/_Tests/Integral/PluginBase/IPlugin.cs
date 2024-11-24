namespace Qenex.QSuite.PluginBase;

public interface IPlugin
{
    string Name { get; set; }
    string Version { get; set; }
    
    void DisplayInfo();
}