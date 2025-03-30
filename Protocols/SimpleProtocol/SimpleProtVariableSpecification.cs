using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.SimpleProtocol;

public class SimpleProtVariableSpecification : ProtVariableSpecification
{
    public SimpleProtVariableSpecification()
    {
        Name = "SimpleProtVariableSpecification";
    }
    
    public static SimpleProtVariableSpecification Create(IVarEvent variableEvent, string commParams)
    {
        var comParameters = commParams.Split(';');
            
        // Multiplier
        var multiplierStr = comParameters.FirstOrDefault(e => e.Contains("multiplier"))?.Split('=')[1].Trim('"');
        var multiplier = int.Parse(multiplierStr!);
            
        // Direction
        var directionStr = comParameters.FirstOrDefault(e => e.Contains("direction"))?.Split('=')[1].Trim('"');
        directionStr = char.ToUpper(directionStr![0]) + directionStr.Substring(1);
        var direction = Enum.Parse<CommDirection>(directionStr);
            
        // Id
        var id = comParameters.FirstOrDefault(e => e.Contains("id"))?.Split('=')[1].Trim('"');
        
        var spec = new SimpleProtVariableSpecification() 
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