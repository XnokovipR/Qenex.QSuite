using Qenex.QSuite.PluginBase;

namespace Qenex.QSuite.APlugin;

public class A1Plugin : Plugin
{
    public A1Plugin()
    {
        Name = "A1";
        Version = "1.0.0";
    }
    public override void DisplayInfo()
    {
        Console.WriteLine($"A1-Plugin: {Name}, Version: {Version}");        
    }
}