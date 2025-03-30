using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.XcpProtocol;

public class XcpVariableSpecification : ProtVariableSpecification
{
    public XcpVariableSpecification()
    {
        Name = "XcpVariableSpecification";
    }

    public static XcpVariableSpecification Create(IVarEvent variableEvent, string commParams)
    {
        var comParameters = commParams.Split(';');
            
        // Multiplier
        var multiplierStr = comParameters.FirstOrDefault(e => e.Contains("multiplier"))?.Split('=')[1].Trim('"');
        var multiplier = int.Parse(multiplierStr!);
            
        // Direction
        var directionStr = comParameters.FirstOrDefault(e => e.Contains("direction"))?.Split('=')[1].Trim('"');
        directionStr = char.ToUpper(directionStr![0]) + directionStr.Substring(1);
        var direction = Enum.Parse<CommDirection>(directionStr);
            
        // Address
        var addressStr = comParameters.FirstOrDefault(e => e.Contains("address"))?.Split('=')[1].Trim('"');
        var address = uint.Parse(addressStr!);
        
        var spec = new XcpVariableSpecification() 
        {
            VariableEvent = variableEvent,
            Multiplier = multiplier,
            Direction = direction,
            Address = address
        };

        return spec;
    }
    
    public IVarEvent VariableEvent { get; set; }
    public uint Address { get; set; }
    public int Multiplier { get; set; }
    public CommDirection Direction { get; set; }
}