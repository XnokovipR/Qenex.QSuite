using System.Globalization;
using Qenex.QSuite.Protocols.XcpCore;

namespace Qenex.QSuite.Protocols.XcpTcpProtocol;

/// <summary>
/// XCP on TCP session configuration parsed from the protocol's RawSettings, e.g.
/// timeoutMs="1000";daqTimestamps="slave". Host and port belong to the TCP Client driver, and
/// byte order comes from the slave's CONNECT response. Empty settings are valid (all defaults).
/// </summary>
public sealed class XcpTcpSessionSettings
{
    /// <summary>Response timeout per command (EV_CMD_PENDING restarts it).</summary>
    public int TimeoutMs { get; init; } = 1000;

    /// <summary>DAQ time axis source: true = ECU timestamps (default), false = PC receive time.</summary>
    public bool UseSlaveDaqTimestamps { get; init; } = true;

    public static XcpTcpSessionSettings Parse(string rawSettings)
    {
        var settings = rawSettings
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1].Trim('"'), StringComparer.OrdinalIgnoreCase);

        return new XcpTcpSessionSettings
        {
            TimeoutMs = ParseTimeout(settings),
            UseSlaveDaqTimestamps = XcpSettingsParsing.ParseUseSlaveDaqTimestamps(settings)
        };
    }

    private static int ParseTimeout(IReadOnlyDictionary<string, string> settings)
    {
        if (!settings.TryGetValue("timeoutMs", out var value))
        {
            return 1000;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout) && timeout > 0
            ? timeout
            : throw new ArgumentException($"Invalid setting timeoutMs='{value}' (expected a positive integer).");
    }
}
