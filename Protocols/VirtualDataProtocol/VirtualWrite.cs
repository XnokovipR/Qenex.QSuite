using Qenex.QSuite.Variables.QVariables;

namespace Qenex.QSuite.Protocols.VirtualDataProtocol;

/// <summary>
/// One script write: which variable and when it was written. The value itself already sits in
/// the variable — the record only carries the sample identity through the buffer, so the
/// published timestamp is the time of the write, not of the (later) processing.
/// </summary>
public readonly record struct VirtualWrite(IVariableBase Variable, DateTime TimestampUtc);
