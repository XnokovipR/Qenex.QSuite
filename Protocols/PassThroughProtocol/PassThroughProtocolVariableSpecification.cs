using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Protocols.PassThroughProtocol;

public class PassThroughProtocolVariableSpecification : ProtVariableSpecification
{
    public PassThroughProtocolVariableSpecification()
    {
        Name = "PassThroughProtocolVariableSpecification";
    }

    public string CommParams { get; init; } = string.Empty;

    public static PassThroughProtocolVariableSpecification Create(string commParams)
    {
        return new PassThroughProtocolVariableSpecification
        {
            CommParams = commParams.Contains("commParams=", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : commParams
        };
    }
}
