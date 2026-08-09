using System.Globalization;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>An event marked as an ECU event channel: its channel number and transfer direction
/// (S2: STIM is configured the same way as DAQ, only with direction="STIM").</summary>
public readonly record struct XcpEventBinding(ushort Channel, bool IsStim);

/// <summary>
/// XCP's reading of the shared <see cref="IVarEvent.EventExtraParams"/> string (same
/// key="value";key="value" syntax as commParams; the event itself does not interpret it).
/// Known XCP keys: direction="DAQ" or direction="STIM" marks the event as an ECU event channel
/// and daqId="N" carries the channel number — they are only valid together. Events without extra
/// params stay ordinary polling timers. Validation is strict on purpose: a typo must fail loudly
/// at project load, never silently degrade a DAQ/STIM variable to polling.
/// </summary>
public static class XcpEventExtraParams
{
    private const string DirectionKey = "direction";
    private const string DaqIdKey = "daqId";

    /// <summary>
    /// Returns the ECU event channel binding when <paramref name="varEvent"/> is marked as a
    /// DAQ or STIM event, null for an ordinary (polling) event. Throws
    /// <see cref="ArgumentException"/> with an exact description on any inconsistency; unknown
    /// keys are only warned about, so future keys of other protocols do not break XCP.
    /// </summary>
    public static XcpEventBinding? GetEventBinding(IVarEvent varEvent, ILogger? logger = null)
    {
        var text = varEvent.EventExtraParams;
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var settings = ParseStrict(text, varEvent.Name);

        foreach (var key in settings.Keys)
        {
            if (!key.Equals(DirectionKey, StringComparison.OrdinalIgnoreCase) &&
                !key.Equals(DaqIdKey, StringComparison.OrdinalIgnoreCase))
            {
                logger?.Log(LogLevel.Warn,
                    $"XCP: event '{varEvent.Name}' eventExtraParams key '{key}' is not known to XCP and is ignored (known keys: {DirectionKey}, {DaqIdKey}).");
            }
        }

        settings.TryGetValue(DirectionKey, out var direction);
        settings.TryGetValue(DaqIdKey, out var daqIdText);

        if (direction == null && daqIdText == null)
        {
            return null;
        }

        if (direction == null)
        {
            throw new ArgumentException(
                $"Event '{varEvent.Name}': eventExtraParams contains {DaqIdKey}=\"{daqIdText}\" but no {DirectionKey}; add {DirectionKey}=\"DAQ\" or remove {DaqIdKey}.");
        }

        var isStim = direction.Equals("STIM", StringComparison.OrdinalIgnoreCase);
        if (!isStim && !direction.Equals("DAQ", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Event '{varEvent.Name}': eventExtraParams {DirectionKey} '{direction}' is not supported (only \"DAQ\" or \"STIM\").");
        }

        if (daqIdText == null)
        {
            throw new ArgumentException(
                $"Event '{varEvent.Name}': eventExtraParams {DirectionKey}=\"{direction.ToUpperInvariant()}\" requires {DaqIdKey}=\"<ECU event channel number>\".");
        }

        if (!ushort.TryParse(daqIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var channel))
        {
            throw new ArgumentException(
                $"Event '{varEvent.Name}': eventExtraParams {DaqIdKey} '{daqIdText}' is not a number in 0..65535.");
        }

        return new XcpEventBinding(channel, isStim);
    }

    /// <summary>Unlike the lenient commParams parser, a segment that is not key="value" throws —
    /// the string cannot be checked anywhere earlier than here, so nothing may slip through.</summary>
    private static Dictionary<string, string> ParseStrict(string text, string eventName)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = item.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[0].Length == 0)
            {
                throw new ArgumentException(
                    $"Event '{eventName}': eventExtraParams segment '{item}' is not in the key=\"value\" form.");
            }

            settings[parts[0]] = parts[1].Trim('"');
        }

        return settings;
    }
}
