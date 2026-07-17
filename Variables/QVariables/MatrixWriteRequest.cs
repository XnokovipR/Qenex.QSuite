#nullable enable
namespace Qenex.QSuite.Variables.QVariables;

/// <summary>
/// One pending element write of a MatrixVariable: the edited bytes at their byte offset within
/// the raw buffer. Queued by the writing host and drained by the protocol that owns the variable.
/// The request carries the bytes themselves so the user's edit survives even when a polled read
/// replaces the raw buffer between the edit and the bus write.
/// </summary>
public class MatrixWriteRequest
{
    public MatrixWriteRequest(int byteOffset, byte[] bytes)
    {
        ByteOffset = byteOffset;
        Bytes = bytes;
    }

    public int ByteOffset { get; }
    public byte[] Bytes { get; }
}
