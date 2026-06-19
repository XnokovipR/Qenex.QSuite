using System.Globalization;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Protocols.RawCanProtocol;

public class RawCanProtocolVariableSpecification : ProtVariableSpecification
{
    public RawCanProtocolVariableSpecification()
    {
        Name = "RawCanProtocolVariableSpecification";
    }

    /// <summary>The 11-bit standard CAN identifier this variable is bound to.</summary>
    public uint CanId { get; init; }

    // commParams carries the variable's address as the CAN id, in hex: address="100" or address="0x100".
    public static RawCanProtocolVariableSpecification Create(string commParams)
    {
        return new RawCanProtocolVariableSpecification
        {
            CanId = ParseCanId(commParams),
        };
    }

    private static uint ParseCanId(string commParams)
    {
        var settings = commParams
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1].Trim('"'), StringComparer.OrdinalIgnoreCase);

        if (!settings.TryGetValue("address", out var value) && !settings.TryGetValue("id", out value))
        {
            return 0;
        }

        value = value.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        return uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id) ? id : 0;
    }
}
