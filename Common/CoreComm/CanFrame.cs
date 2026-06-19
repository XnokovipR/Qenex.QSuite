namespace Qenex.QSuite.Common.CoreComm;

/// <summary>
/// A single CAN frame — the transport-independent payload exchanged between a CAN driver
/// and the protocol layered on top of it (e.g. XCP, J1939). This is the transport type
/// used as <c>T</c> in <c>ProtocolBase&lt;CanFrame&gt;</c> for CAN-based protocols.
/// </summary>
public sealed class CanFrame
{
    /// <summary>11-bit (standard) or 29-bit (extended) CAN identifier.</summary>
    public uint CanId { get; }

    /// <summary>True for a 29-bit extended identifier, false for an 11-bit standard identifier.</summary>
    public bool IsExtended { get; }

    /// <summary>Frame payload. For classic CAN this is 0..8 bytes.</summary>
    public byte[] Data { get; }

    /// <summary>Receive timestamp in microseconds; 0 for frames that are about to be sent.</summary>
    public ulong TimestampMicroseconds { get; }

    public CanFrame(uint canId, byte[] data, bool isExtended = false, ulong timestampMicroseconds = 0)
    {
        CanId = canId;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        IsExtended = isExtended;
        TimestampMicroseconds = timestampMicroseconds;
    }
}
