using System.Reflection;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Protocols.XcpProtocol;

public class Xcp : ProtocolBase
{
    public Xcp()
    {
        Specification = new SpecificationBase()
        {
            Name = "XCP Protocol",
            Description = "XCP Protocol",
            CreatedOn = new DateTime(2021, 11, 23),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("Version not found"),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public override IProtocolVariable CreateProtocolVariable(IVariableBase variable, string additionalData)
    {
        var protocolVariable = new XcpProtocolVariable
        {
            Variable = variable
        };
        
        var separatedData = additionalData.Split(';');
        
        foreach (var s in separatedData)
        {
            var data = s.Split(':');
            if (data.Length == 2)
            {
                switch (data[0].ToLower())
                {
                    case "address":
                        protocolVariable.Address = data[1];
                        break;
                    case "event":
                        protocolVariable.Event = data[1];
                        break;
                }
            }
        }

        return protocolVariable;
    }
}