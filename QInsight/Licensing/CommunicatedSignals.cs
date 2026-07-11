using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Modules.Module;

namespace Qenex.QInsight.Licensing;

/// <summary>Free-tier limit on communicated signals: counting and messaging. The limit itself is
/// baked into the binary (LicensingConstants.FreeMaxCommunicatedSignals); the enforcement
/// comparison stays inline at every call site (no single patchable check method).</summary>
public static class CommunicatedSignals
{
    /// <summary>Counts protocol variables marked as communicated across the whole project —
    /// communication drivers only; the data-logger/replay infrastructure mirrors project
    /// variables as its own protocol variables and would inflate the count.</summary>
    public static int Count(IModuleBase? module) => Count(module?.Drivers);

    public static int Count(IEnumerable<IDriverBase>? drivers) =>
        drivers?
            .Where(IsCommunicationDriver)
            .SelectMany(driver => driver.Protocols)
            .SelectMany(protocol => protocol.Variables)
            .Count(variable => variable.IsCommunicated) ?? 0;

    /// <summary>True for drivers that communicate with the outside world. Same rule as the
    /// source-driver filter in project configuration (interface markers + specification-name
    /// fallback for plugin builds that predate the marker interfaces).</summary>
    public static bool IsCommunicationDriver(IDriverBase driver) =>
        driver is not IProtocolVariableSinkDriver
        && driver is not IReplayDriver
        && !HasSpecificationName(driver, "FileDataLoggerDriver")
        && !HasSpecificationName(driver, "FileDataReplayDriver");

    private static bool HasSpecificationName(IDriverBase driver, string name) =>
        string.Equals(driver.Specification?.Name, name, StringComparison.OrdinalIgnoreCase);

    /// <summary>Maximum communicated signals for the current license; null = unlimited.
    /// Only the Free tier is limited.</summary>
    public static int? GetLimit(LicenseService licenseService) =>
        licenseService.IsFreeTier ? LicensingConstants.FreeMaxCommunicatedSignals : null;

    public static string BuildOverLimitMessage(int count, int limit) =>
        $"Free license: the project has {count} communicated signals, the limit is {limit}.";
}
