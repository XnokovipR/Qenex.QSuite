using Qenex.QSuite.Common.PluginManager;
using Qenex.QSuite.ModuleXmlHandler.XmlStructure;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.ModuleXmlHandler;

/// <summary>
/// Read-only consistency checks of a deserialized project file: format version,
/// resolvability of driver/protocol plugin references, driver-protocol transport
/// compatibility and variable references. Findings are informational — the caller
/// decides how to surface them and loading always continues.
/// </summary>
public static class XmlModuleValidator
{
    public static IReadOnlyList<string> Validate(
        XmlModule xmlModule,
        IList<PluginDetails> driverDetails,
        IList<PluginDetails> protocolDetails)
    {
        var findings = new List<string>();

        ValidateFormatVersion(xmlModule, findings);

        var variableIds = xmlModule.Variables.Select(v => v.Id).ToHashSet();

        foreach (var driverRef in xmlModule.DriverReferences)
        {
            var xmlDriver = xmlModule.Drivers.FirstOrDefault(d => d.Name == driverRef.Ref);
            if (xmlDriver == null)
            {
                findings.Add($"Driver \"{driverRef.Ref}\" is referenced but missing in the drivers list.");
                continue;
            }

            var driverPlugin = ResolvePlugin(driverDetails, "Driver", xmlDriver.Name, xmlDriver.Version, findings);

            foreach (var protocolRef in driverRef.ProtocolReferences)
            {
                var xmlProtocol = xmlModule.Protocols.FirstOrDefault(p => p.Name == protocolRef.Ref);
                if (xmlProtocol == null)
                {
                    findings.Add($"Protocol \"{protocolRef.Ref}\" is referenced but missing in the protocols list.");
                    continue;
                }

                var protocolPlugin = ResolvePlugin(protocolDetails, "Protocol", xmlProtocol.Name, xmlProtocol.Version, findings);

                if (driverPlugin != null && protocolPlugin != null)
                {
                    ValidateCompatibility(driverPlugin, protocolPlugin, findings);
                }

                foreach (var variableRef in protocolRef.VariableReferences)
                {
                    if (!variableIds.Contains(variableRef.Ref))
                    {
                        findings.Add($"Protocol \"{DisplayName(xmlProtocol.Label, xmlProtocol.Name)}\" references variable id {variableRef.Ref} which does not exist in the project.");
                    }
                }
            }
        }

        return findings;
    }

    private static void ValidateFormatVersion(XmlModule xmlModule, List<string> findings)
    {
        if (string.IsNullOrWhiteSpace(xmlModule.FormatVersion))
        {
            findings.Add("The project file has no format version — it was saved by an older application version. Re-saving the project will upgrade it.");
            return;
        }

        if (!Version.TryParse(xmlModule.FormatVersion, out var formatVersion))
        {
            findings.Add($"The project file format version \"{xmlModule.FormatVersion}\" is not a valid version number.");
            return;
        }

        if (formatVersion > Version.Parse(XmlModule.CurrentFormatVersion))
        {
            findings.Add($"The project file format version {formatVersion} is newer than this application supports ({XmlModule.CurrentFormatVersion}) — some content may not load correctly.");
        }
    }

    private static PluginDetails? ResolvePlugin(
        IList<PluginDetails> plugins,
        string pluginKind,
        string name,
        string version,
        List<string> findings)
    {
        if (!Version.TryParse(version, out var parsedVersion))
        {
            findings.Add($"{pluginKind} \"{name}\" has an invalid version \"{version}\" in the project file.");
            return null;
        }

        var plugin = plugins.FirstOrDefault(p => p.Name == name
                                                 && p.Version.Major.Equals(parsedVersion.Major)
                                                 && p.Version.Minor.Equals(parsedVersion.Minor));
        if (plugin == null)
        {
            findings.Add($"{pluginKind} plugin \"{name}\" v{version} is not installed — the component will be skipped.");
        }

        return plugin;
    }

    private static void ValidateCompatibility(PluginDetails driverPlugin, PluginDetails protocolPlugin, List<string> findings)
    {
        var driverName = DisplayName(driverPlugin.Label, driverPlugin.Name);
        var protocolName = DisplayName(protocolPlugin.Label, protocolPlugin.Name);

        if (!TransportTypes.AreCompatible(driverPlugin.Transports, protocolPlugin.Transports))
        {
            findings.Add($"Protocol \"{protocolName}\" is not compatible with driver \"{driverName}\" (different transport type) — it will not receive any data.");
        }
        else if (protocolPlugin.CompatibleDrivers.Count > 0
                 && !protocolPlugin.CompatibleDrivers.Contains(driverPlugin.Name, StringComparer.OrdinalIgnoreCase))
        {
            findings.Add($"Protocol \"{protocolName}\" is designed for a different driver than \"{driverName}\" — it may not work correctly.");
        }
    }

    private static string DisplayName(string label, string name)
    {
        return string.IsNullOrWhiteSpace(label) ? name : label;
    }
}
