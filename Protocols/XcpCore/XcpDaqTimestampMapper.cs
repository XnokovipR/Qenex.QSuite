using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>
/// Maps slave DAQ timestamps (carried in ODT 0 of every cycle) onto the host UTC axis
/// (D5 stages 4b/4c). The first timestamped packet anchors the slave clock to its receive
/// time; all following samples keep their slave-side spacing, so the time axis stays
/// equidistant regardless of transport batching. The raw counter is unwrapped on overflow
/// (the transport delivers DTOs in order). When the mapped time drifts more than
/// <see cref="ReanchorThresholdSeconds"/> from the receive time (slave reboot, long transport
/// stall), the mapper re-anchors and notes it in the log. ODTs 1..n of a cycle carry no
/// timestamp; they reuse the cycle time of the last ODT 0 seen for their DAQ list.
/// </summary>
public sealed class XcpDaqTimestampMapper(int timestampSize, double tickSeconds, int listCount, ILogger? logger = null)
{
    private const double ReanchorThresholdSeconds = 2.0;

    private readonly ulong wrapModulus = 1UL << (Math.Min(timestampSize, 4) * 8);
    private readonly DateTime?[] lastCycleUtc = new DateTime?[listCount];

    private bool anchored;
    private DateTime anchorHostUtc;
    private double anchorSlaveSeconds;
    private ulong unwrappedHigh;
    private uint lastRaw;
    private long reanchors;

    /// <summary>
    /// Returns the sample time for one decoded DTO: the mapped slave time when the packet
    /// carried a timestamp, the last cycle time of the same DAQ list for timestamp-less
    /// ODTs 1..n, and the receive time as the last resort.
    /// </summary>
    public DateTime Map(int listIndex, uint? timestampRaw, DateTime receivedUtc)
    {
        if (timestampRaw is not { } raw)
        {
            return lastCycleUtc[listIndex] ?? receivedUtc;
        }

        if (raw < lastRaw)
        {
            unwrappedHigh += wrapModulus; // in-order transport: a smaller raw value means the counter overflowed
        }

        lastRaw = raw;

        var slaveSeconds = (unwrappedHigh + raw) * tickSeconds;
        if (!anchored)
        {
            Anchor(slaveSeconds, receivedUtc);
        }

        var mapped = anchorHostUtc.AddSeconds(slaveSeconds - anchorSlaveSeconds);
        if (Math.Abs((mapped - receivedUtc).TotalSeconds) > ReanchorThresholdSeconds)
        {
            reanchors++;
            logger?.Log(LogLevel.Info,
                $"XCP: DAQ timestamps re-anchored to the receive time ({reanchors}× so far) — " +
                "slave restart or a transport stall shifted the clocks by more than " +
                $"{ReanchorThresholdSeconds:0.#} s.");
            Anchor(slaveSeconds, receivedUtc);
            mapped = receivedUtc;
        }

        lastCycleUtc[listIndex] = mapped;
        return mapped;
    }

    private void Anchor(double slaveSeconds, DateTime hostUtc)
    {
        anchored = true;
        anchorSlaveSeconds = slaveSeconds;
        anchorHostUtc = hostUtc;
    }
}
