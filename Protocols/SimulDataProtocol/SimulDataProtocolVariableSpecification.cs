using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.SimulDataProtocol;

public class SimulDataProtocolVariableSpecification : ProtVariableSpecification
{
    public SimulDataProtocolVariableSpecification()
    {
        Name = "SimulDataProtocolVariableSpecification";
    }

    public static SimulDataProtocolVariableSpecification CreateDefault(IVarEvent variableEvent, string id)
    {
        var spec = new SimulDataProtocolVariableSpecification() 
        {
            VariableEvent = variableEvent,
            Multiplier = 1,
            Direction = CommDirection.Read,
            Id = id
        };
        return spec;
    }
    
    public static SimulDataProtocolVariableSpecification Create(IVarEvent variableEvent, string commParams)
    {
        var comParameters = commParams.Split(';');
            
        // Multiplier
        var multiplierStr = comParameters.FirstOrDefault(e => e.Contains("multiplier"))?.Split('=')[1].Trim('"');
        var multiplier = string.IsNullOrWhiteSpace(multiplierStr)
            ? 1
            : int.Parse(multiplierStr);
            
        // Direction
        var directionStr = comParameters.FirstOrDefault(e => e.Contains("direction"))?.Split('=')[1].Trim('"');
        directionStr = string.IsNullOrWhiteSpace(directionStr)
            ? CommDirection.Read.ToString()
            : char.ToUpper(directionStr[0]) + directionStr.Substring(1);
        var direction = Enum.Parse<CommDirection>(directionStr);
            
        // Id
        var id = comParameters.FirstOrDefault(e => e.Contains("id"))?.Split('=')[1].Trim('"') ?? string.Empty;
        
        var spec = new SimulDataProtocolVariableSpecification() 
        {
            VariableEvent = variableEvent,
            Multiplier = multiplier,
            Direction = direction,
            Id = id
        };

        return spec;
    }
    
    
    public IVarEvent VariableEvent { get; set; }
    public string Id { get; set; }
    public int Multiplier { get; set; }
    public CommDirection Direction { get; set; }
}
