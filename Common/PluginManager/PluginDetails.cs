namespace Qenex.QSuite.Common.PluginManager;

public class PluginDetails
{
    public string Name { get; set; } = string.Empty;
    // User-facing name from the plugin specification; Name stays the stable
    // technical id stored in .qproj files.
    public string Label { get; set; } = string.Empty;
    public Version Version { get; set; } = null!;
    public string PathName { get; set; } = string.Empty;
}