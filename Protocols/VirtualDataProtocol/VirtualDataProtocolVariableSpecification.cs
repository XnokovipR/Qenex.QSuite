using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Protocols.VirtualDataProtocol;

/// <summary>
/// Specification of one virtual variable. There is nothing to address — the module routes
/// script writes by variable identity — and no event to reference — samples are born from the
/// writes themselves. The optional "id" only keeps the project file readable. The property
/// matches the commParam key 1:1, so the inherited reflection-based ToCommParam round-trips.
/// </summary>
public class VirtualDataProtocolVariableSpecification : ProtVariableSpecification
{
    public VirtualDataProtocolVariableSpecification()
    {
        Name = "VirtualDataProtocolVariableSpecification";
    }

    public string Id { get; set; } = string.Empty;

    public static VirtualDataProtocolVariableSpecification Create(string commParams)
    {
        // Tolerant on purpose: every parameter is optional and unknown keys are ignored,
        // so a commParam written for another protocol can never fail the project load.
        var parameters = commParams
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(parameter => parameter.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim().Trim('"'), StringComparer.OrdinalIgnoreCase);

        return new VirtualDataProtocolVariableSpecification
        {
            Id = parameters.GetValueOrDefault("id", string.Empty)
        };
    }
}
