using Peak.Can.Basic;

namespace Qenex.QSuite.Drivers.PeakCanDriver;

/// <summary>
/// Single exception type for PeakCAN driver failures, carrying the originating PCAN-Basic status.
/// By design there is no per-status exception hierarchy — human-readable text is obtained from
/// <see cref="PCANBasic.GetErrorText"/> and surfaced via the logger / communication state.
/// </summary>
public sealed class PeakCanException : Exception
{
    public TPCANStatus Status { get; }

    public PeakCanException(TPCANStatus status, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
    }
}
