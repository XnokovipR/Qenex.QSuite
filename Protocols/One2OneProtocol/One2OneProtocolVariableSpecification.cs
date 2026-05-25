using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Protocols.One2OneProtocol;

public class One2OneProtocolVariableSpecification : ProtVariableSpecification
{
    public One2OneProtocolVariableSpecification()
    {
        Name = "One2OneProtocolVariableSpecification";
    }

    public string CommParams { get; init; } = string.Empty;

    public static One2OneProtocolVariableSpecification Create(string commParams)
    {
        return new One2OneProtocolVariableSpecification
        {
            CommParams = commParams
        };
    }
}
