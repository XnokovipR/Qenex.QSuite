using System.ComponentModel.Design;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Protocols.Protocol;

public interface IProtVariableSpecification
{
    string Name { get; }

    /// <summary>
    /// Serializes the specification back to the commParam string shown and edited in Project
    /// Configuration. Must round-trip with the protocol's CreateProtocolVariable parsing. The
    /// serialization lives in the specification itself so a protocol plugin dropped into the
    /// Protocols folder works without any change in the host application.
    /// </summary>
    string ToCommParam();
}