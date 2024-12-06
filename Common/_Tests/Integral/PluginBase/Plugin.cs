namespace Qenex.QSuite.PluginBase;

public class Plugin : IPlugin
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    
    public virtual void DisplayInfo()
    {
        Console.WriteLine($"Plugin: {Name}, Version: {Version}");
    }
}