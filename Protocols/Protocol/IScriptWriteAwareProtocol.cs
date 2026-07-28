using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Protocols.Protocol;

/// <summary>
/// A protocol whose communicated variables are fed by script writes: the module routes every
/// successful script write of such a variable here (the scripting engine itself knows nothing
/// about protocols). The implementation must not block — the expected shape is
/// enqueue-into-buffer with a consumer loop publishing the notifications.
/// </summary>
public interface IScriptWriteAwareProtocol
{
    void OnVariableWrittenByScript(IVariableBase variable);
}
