using Qenex.QSuite.PluginBase;

namespace Qenex.QSuite.BPlugin;

public class B1Plugin : Plugin
{
    public B1Plugin()
    {
        Name = "B1";
        Version = "1.0.0";
    }
    public override void DisplayInfo()
    {
        Console.WriteLine($"B1-Plugin: {Name}, Version: {Version}");
    }
}