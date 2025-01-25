using System.Reflection;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.QVariables;
using Qenex.QSuite.Specification;
using Qenex.QSuite.VariableEvents;

namespace Qenex.QSuite.Protocols.SimpleProtocol;

public class SimpleOne2OneProtocol : ProtocolBase
{
    public SimpleOne2OneProtocol()
    {
        Specification = new SpecificationBase()
        {
            Name = "SimpleO2OProtocol",
            Label = "Simple One to One Protocol",
            Description = "No conversion of data is done. Data is sent as is.",
            CreatedOn = new DateTime(2021, 11, 23),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("Version not found"),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated)
    {
        throw new NotSupportedException();
    }
    
    public override IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents,
        string commParams, bool isCommunicated )
    {
        try
        {
            var eventRefName = commParams.Split(';').FirstOrDefault(e => e.Contains("eventRef"));
            eventRefName = eventRefName?.Split('=')[1].Trim('"');
        
            var varEvent = variableEvents.FirstOrDefault(e => e.Name == eventRefName);
        
            var protocolVariable = new SimpleOne2OneProtocolVariable
            {
                Variable = variable,
                IsCommunicated = isCommunicated,
                ProtocolVariableSpecification = SimpleProtVariableSpecification.Create(varEvent!, commParams)
            };

            return protocolVariable; 
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Warn, $"Protocol variable specification for variable {variable.Name} could not be created (${e.Message}).");
            return null;
        }
       
    }

}