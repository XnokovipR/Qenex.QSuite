namespace Qenex.QSuite.PluginBase;

public class Plugin : IPlugin
{
    public string Name { get; set; }
    public string Version { get; set; }
    
    public virtual void DisplayInfo()
    {
        Console.WriteLine($"Plugin: {Name}, Version: {Version}");
    }
}