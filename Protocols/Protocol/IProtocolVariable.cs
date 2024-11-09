using Qenex.QSuite.Variable;

namespace Qenex.QSuite.Protocol;

/// <summary>
/// Protocol variable interface is a IVariableBase extended about the properties
/// how the variable is communicated via protocol (e.g. address in communicated device,
/// or nothing for logging into DB - it is up to the protocol). 
/// </summary>
public interface IProtocolVariable : IVariableBase
{
    
}