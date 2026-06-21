using System.Globalization;
using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Protocols.RawCanProtocol;

/// <summary>Byte order used when assembling a multi-byte value from CAN frame bytes.</summary>
public enum RawCanByteOrder { LittleEndian, BigEndian }

public class RawCanProtocolVariableSpecification : ProtVariableSpecification
{
    public RawCanProtocolVariableSpecification()
    {
        Name = "RawCanProtocolVariableSpecification";
    }

    /// <summary>The 11-bit standard CAN identifier this variable is bound to.</summary>
    public uint CanId { get; init; }

    /// <summary>Index of the first frame data byte the value is read from (default 0).</summary>
    public int Offset { get; init; }

    /// <summary>Byte order used to assemble multi-byte values (default little-endian).</summary>
    public RawCanByteOrder ByteOrder { get; init; } = RawCanByteOrder.LittleEndian;

    /// <summary>
    /// Controlled serialization so the configuration round-trips: emitted as
    /// canId="0x1A";offset="0";byteOrder="le" and read back by <see cref="Create"/>. Without this the
    /// generic reflective serializer would write the properties in decimal under keys/formats the
    /// parser does not understand, corrupting the configuration on reload.
    /// </summary>
    public string CommParams =>
        $"canId=\"0x{CanId:X}\";offset=\"{Offset}\";byteOrder=\"{(ByteOrder == RawCanByteOrder.BigEndian ? "be" : "le")}\"";

    // commParam example: canId="0x100";offset="0";byteOrder="le" (offset/byteOrder optional: default 0 / le).
    public static RawCanProtocolVariableSpecification Create(string commParams)
    {
        var settings = Parse(commParams);
        return new RawCanProtocolVariableSpecification
        {
            CanId = ParseCanId(settings),
            Offset = ParseOffset(settings),
            ByteOrder = ParseByteOrder(settings),
        };
    }

    private static Dictionary<string, string> Parse(string commParams)
    {
        return commParams
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1].Trim('"'), StringComparer.OrdinalIgnoreCase);
    }

    private static uint ParseCanId(IReadOnlyDictionary<string, string> settings)
    {
        if (!settings.TryGetValue("canId", out var value))
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

    private static int ParseOffset(IReadOnlyDictionary<string, string> settings)
    {
        return settings.TryGetValue("offset", out var value)
               && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var offset)
               && offset >= 0
            ? offset
            : 0;
    }

    private static RawCanByteOrder ParseByteOrder(IReadOnlyDictionary<string, string> settings)
    {
        return settings.TryGetValue("byteOrder", out var value)
               && value.Trim().Equals("be", StringComparison.OrdinalIgnoreCase)
            ? RawCanByteOrder.BigEndian
            : RawCanByteOrder.LittleEndian;
    }
}
