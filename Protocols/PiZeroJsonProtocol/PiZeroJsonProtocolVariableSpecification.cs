using System.Globalization;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.PiZeroJsonProtocol;

public class PiZeroJsonProtocolVariableSpecification : ProtVariableSpecification
{
    public PiZeroJsonProtocolVariableSpecification()
    {
        Name = "PiZeroJsonProtocolVariableSpecification";
    }

    public string Id { get; set; } = string.Empty;
    public string Param { get; set; } = string.Empty;
    public int Multiplier { get; set; } = 1;
    public CommDirection Direction { get; set; } = CommDirection.Read;
    public IVarEvent? VariableEvent { get; set; }

    public static PiZeroJsonProtocolVariableSpecification Create(string commParams)
    {
        return Create(null, commParams);
    }

    public static PiZeroJsonProtocolVariableSpecification Create(IVarEvent? variableEvent, string commParams)
    {
        var parameters = ParseParameters(commParams);
        return new PiZeroJsonProtocolVariableSpecification
        {
            VariableEvent = variableEvent,
            Id = parameters.TryGetValue("id", out var id) ? id : string.Empty,
            Param = parameters.TryGetValue("param", out var param) ? param : string.Empty,
            Multiplier = parameters.TryGetValue("multiplier", out var multiplierText)
                         && int.TryParse(multiplierText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var multiplier)
                ? multiplier
                : 1,
            Direction = parameters.TryGetValue("direction", out var directionText)
                        && Enum.TryParse<CommDirection>(directionText, ignoreCase: true, out var direction)
                ? direction
                : CommDirection.Read
        };
    }

    private static Dictionary<string, string> ParseParameters(string commParams)
    {
        return commParams
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(parameter => parameter.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => parts[0],
                parts => parts[1].Trim('"'),
                StringComparer.OrdinalIgnoreCase);
    }
}
