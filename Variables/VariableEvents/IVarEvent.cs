namespace Qenex.QSuite.Variables.VariableEvents;

public interface IVarEvent
{
    string Name { get; set; }

    /// <summary>
    /// Optional protocol-specific parameters in the commParams syntax (key="value";key="value").
    /// The event itself does not interpret them — the protocol a variable is bound to parses the
    /// keys it understands (e.g. XCP: direction="DAQ";daqId="3"). Empty for ordinary events.
    /// </summary>
    string EventExtraParams { get; set; }
}