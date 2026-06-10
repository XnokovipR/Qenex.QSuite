using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Protocols.JsonSignalProtocol;

public class JsonSignalProtocolVariableSpecification : ProtVariableSpecification
{
    public JsonSignalProtocolVariableSpecification()
    {
        Name = "JsonSignalProtocolVariableSpecification";
    }

    public string SignalName { get; init; } = string.Empty;
    public string CommParams { get; init; } = string.Empty;

    public static JsonSignalProtocolVariableSpecification Create(string commParams, string variableName)
    {
        var settings = ParseSettings(commParams);
        var signalName = GetString(settings, "name",
            GetString(settings, "signal",
                GetString(settings, "signalName",
                    GetString(settings, "id", variableName))));
        return new JsonSignalProtocolVariableSpecification
        {
            SignalName = signalName,
            CommParams = $"id=\"{signalName}\""
        };
    }

    private static Dictionary<string, string> ParseSettings(string rawSettings)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parts in rawSettings
                     .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
                     .Where(parts => parts.Length == 2))
        {
            settings[parts[0]] = parts[1].Trim('"');
        }

        return settings;
    }

    private static string GetString(IReadOnlyDictionary<string, string> settings, string key, string defaultValue)
    {
        return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
    }
}
