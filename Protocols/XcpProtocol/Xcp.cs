using System.Reflection;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.Specification;
using Qenex.QSuite.VariableEvents;

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
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("Version not found."),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public override void SetConfiguration(string rawSettings, string rawEncryptedSettings)
    {
        
    }
    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated)
    {
        throw new NotSupportedException();
    }
    
    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents,
        string commParams, bool isCommunicated)
    {
        try
        {
            var eventRefName = commParams.Split(';').FirstOrDefault(e => e.Contains("eventRef"));
            eventRefName = eventRefName?.Split('=')[1].Trim('"');
        
            var varEvent = variableEvents.FirstOrDefault(e => e.Name == eventRefName);
        
            var protocolVariable = new XcpProtocolVariable
            {
                Variable = variable, 
                IsCommunicated = isCommunicated,
                ProtocolVariableSpecification = XcpVariableSpecification.Create(varEvent!, commParams)
            };

            return protocolVariable; 
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Warn, $"Protocol variable specification for variable {variable.Name} could not be created (${e.Message}).");
            return null;
        }
    }

    public override IEnumerable<T> Encode<T>(IEnumerable<IProtocolVariable> protocolVariables)
    {
        if (typeof(T) != typeof(byte))
        {
            throw new NotSupportedException();
        }
        
        return (new byte[] { 1, 2, 3 } as IEnumerable<T>)!;
    }

    public override IEnumerable<IProtocolVariable> Decode<T>(IEnumerable<T> data)
    {
        if (typeof(T) != typeof(byte))
        {
            throw new NotSupportedException();
        }
        
        return new List<IProtocolVariable>();
    }
}