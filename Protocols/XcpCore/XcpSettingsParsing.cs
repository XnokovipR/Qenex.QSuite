namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>Parsing of XCP protocol settings shared by all transports (CAN, TCP).</summary>
public static class XcpSettingsParsing
{
    /// <summary>
    /// Parses the optional daqTimestamps setting: "slave" (default) puts DAQ samples onto the
    /// time axis of the ECU clock, "master" stamps them with the PC receive time. Maps onto the
    /// standard SET_DAQ_LIST_MODE timestamp bit: with "master" the bit stays clear and a QFW SDK
    /// slave transfers no timestamps (full ODT 0 capacity); a legacy TIMESTAMP_FIXED slave still
    /// sends them and they are ignored.
    /// </summary>
    public static bool ParseUseSlaveDaqTimestamps(IReadOnlyDictionary<string, string> settings)
    {
        if (!settings.TryGetValue("daqTimestamps", out var value))
        {
            return true;
        }

        return value.ToLowerInvariant() switch
        {
            "slave" => true,
            "master" => false,
            _ => throw new ArgumentException($"Invalid setting daqTimestamps='{value}' (expected slave or master).")
        };
    }
}
