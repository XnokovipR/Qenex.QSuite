namespace Qenex.QSuite.Drivers.Driver;

/// <summary>Ambient environment shared by all drivers. The host application sets
/// <see cref="DataRootDirectory"/> at startup; drivers resolve relative data paths against it
/// instead of the application directory, which is not writable under Program Files.</summary>
public static class DriverEnvironment
{
    public static string DataRootDirectory { get; set; } = AppContext.BaseDirectory;
}
