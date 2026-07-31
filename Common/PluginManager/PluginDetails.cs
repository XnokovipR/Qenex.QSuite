namespace Qenex.QSuite.Common.PluginManager;

public class PluginDetails
{
    public string Name { get; set; } = string.Empty;
    // User-facing name from the plugin specification; Name stays the stable
    // technical id stored in .qproj files.
    public string Label { get; set; } = string.Empty;
    public Version Version { get; set; } = null!;
    public string PathName { get; set; } = string.Empty;
    // Transport payload types the plugin declares (driver: ITransportSource<T>,
    // protocol: ProtocolBase<T>); empty = undeclared, treated as unrestricted.
    public IReadOnlyList<Type> Transports { get; set; } = [];
    // Driver Names a protocol narrows itself to (protocols designed for one concrete
    // driver); empty = any type-compatible driver. Always empty for drivers/controls.
    public IReadOnlyList<string> CompatibleDrivers { get; set; } = [];
}