namespace Qenex.QSuite.Protocols.XcpProtocol;

/// <summary>The slave answered a command with a negative response (PID 0xFE).</summary>
public class XcpErrorException(byte errorCode) : Exception(XcpErrorCode.Describe(errorCode))
{
    public byte ErrorCode { get; } = errorCode;
}

/// <summary>No response arrived within the configured timeout (after SYNCH recovery and retries).</summary>
public class XcpTimeoutException(string command) : Exception($"XCP command {command} timed out.")
{
    public string Command { get; } = command;
}

/// <summary>Protocol-level failure: malformed response, unsupported slave property, locked resource.</summary>
public class XcpProtocolException(string message) : Exception(message);

/// <summary>The slave has a property this implementation does not support (e.g. AG&gt;1).
/// Reconnecting cannot help — the session must stop with a clear error.</summary>
public class XcpUnsupportedSlaveException(string message) : XcpProtocolException(message);
