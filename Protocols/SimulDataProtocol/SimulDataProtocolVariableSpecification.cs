using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.SimulDataProtocol;

public class SimulDataProtocolVariableSpecification : ProtVariableSpecification
{
    public SimulDataProtocolVariableSpecification()
    {
        Name = "SimulDataProtocolVariableSpecification";
    }

    public IVarEvent? VariableEvent { get; set; }
    public string Id { get; set; } = string.Empty;

    /// <summary>Signal catalog key (see <see cref="SimulSignalCatalog"/>); empty = fallback by value type.</summary>
    public string Signal { get; set; } = string.Empty;

    public CommDirection Direction { get; set; } = CommDirection.Read;

    public static SimulDataProtocolVariableSpecification CreateDefault(IVarEvent? variableEvent, string id)
    {
        return new SimulDataProtocolVariableSpecification
        {
            VariableEvent = variableEvent,
            Direction = CommDirection.Read,
            Id = id
        };
    }

    public static SimulDataProtocolVariableSpecification Create(IVarEvent? variableEvent, string commParams)
    {
        var parameters = ParseCommParams(commParams);

        var direction = CommDirection.Read;
        if (parameters.TryGetValue("direction", out var directionStr) && !string.IsNullOrWhiteSpace(directionStr))
        {
            direction = Enum.Parse<CommDirection>(directionStr, ignoreCase: true);
        }

        return new SimulDataProtocolVariableSpecification
        {
            VariableEvent = variableEvent,
            Direction = direction,
            Id = parameters.GetValueOrDefault("id", string.Empty),
            Signal = parameters.GetValueOrDefault("signal", string.Empty).ToLowerInvariant()
        };
    }

    private static Dictionary<string, string> ParseCommParams(string commParams)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parts in commParams
                     .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(parameter => parameter.Split('=', 2))
                     .Where(parts => parts.Length == 2))
        {
            parameters[parts[0].Trim()] = parts[1].Trim().Trim('"');
        }

        return parameters;
    }
}
