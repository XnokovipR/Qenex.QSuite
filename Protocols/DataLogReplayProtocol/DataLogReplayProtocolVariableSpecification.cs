using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Protocols.DataLogReplayProtocol;

public class DataLogReplayProtocolVariableSpecification : ProtVariableSpecification
{
    public DataLogReplayProtocolVariableSpecification()
    {
        Name = "DataLogReplayProtocolVariableSpecification";
    }

    public string CommParams { get; init; } = string.Empty;

    public static DataLogReplayProtocolVariableSpecification Create(string commParams)
    {
        return new DataLogReplayProtocolVariableSpecification
        {
            CommParams = commParams
        };
    }
}
