using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Examples.TempSensorProtocol;

/// <summary>
/// Addressing of one variable: which device channel it reads (or writes) and in which direction.
/// The properties match the commParam keys 1:1 ("channel", "direction"), so the inherited
/// reflection-based ToCommParam() round-trips the specification without an override.
/// </summary>
public class TempSensorVariableSpecification : ProtVariableSpecification
{
    public TempSensorVariableSpecification()
    {
        Name = "TempSensorVariableSpecification";
    }

    /// <summary>Channel key as sent by the device, e.g. "ch1".</summary>
    public string Channel { get; set; } = string.Empty;

    public CommDirection Direction { get; set; } = CommDirection.Read;

    public static TempSensorVariableSpecification Create(string commParams)
    {
        var parameters = commParams
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(parameter => parameter.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim().Trim('"'), StringComparer.OrdinalIgnoreCase);

        if (!parameters.TryGetValue("channel", out var channel) || string.IsNullOrWhiteSpace(channel))
        {
            throw new ArgumentException("Missing mandatory commParam 'channel'.");
        }

        var direction = CommDirection.Read;
        if (parameters.TryGetValue("direction", out var directionText) && !string.IsNullOrWhiteSpace(directionText))
        {
            direction = Enum.Parse<CommDirection>(directionText, ignoreCase: true);
        }

        return new TempSensorVariableSpecification
        {
            Channel = channel,
            Direction = direction
        };
    }
}
