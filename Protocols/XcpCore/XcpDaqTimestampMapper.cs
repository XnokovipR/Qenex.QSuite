using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>
/// Maps slave DAQ timestamps (carried in ODT 0 of every cycle) onto the host UTC axis
/// (D5 stages 4b/4c). The first timestamped packet anchors the slave clock to its receive
/// time; all following samples keep their slave-side spacing, so the time axis stays
/// equidistant regardless of transport batching. The raw counter is unwrapped on overflow
/// (the transport delivers DTOs in order). Slave-vs-host clock rate error (the XCP standard
/// does not guarantee slave clock accuracy) is absorbed by a bounded slew: mapped time is
/// pulled towards the receive time whenever it drifts beyond a small dead band, at most
/// <see cref="MaxSlewRate"/> of the receive-time progress, so the correction is invisible
/// in the sample spacing. Only a shift larger than <see cref="ReanchorThresholdSeconds"/>
/// (slave reboot, long transport stall) re-anchors hard and notes it in the log. The
/// emitted axis never runs backwards - consumers (charts, logs) can rely on monotonic
/// time. ODTs 1..n of a cycle carry no timestamp; they reuse the cycle time of the last
/// ODT 0 seen for their DAQ list.
/// </summary>
public sealed class XcpDaqTimestampMapper(int timestampSize, double tickSeconds, int listCount, ILogger? logger = null)
{
    private const double ReanchorThresholdSeconds = 2.0;

    /// <summary>Drift below this offset from the receive time is left untouched - it keeps
    /// the slave-side spacing exact for transport bursts and receive jitter.</summary>
    private const double DriftDeadBandSeconds = 0.25;

    /// <summary>Upper bound of the drift correction as a fraction of the receive-time
    /// progress; must stay well above any real clock error (percent range) and well below
    /// anything visible in the sample spacing.</summary>
    private const double MaxSlewRate = 0.05;

    /// <summary>Sustained drift rate worth a diagnostic log line (0.3 % ≈ far beyond any
    /// crystal tolerance - points to a wrong clock source or conversion on the slave).</summary>
    private const double DriftReportRate = 0.003;

    private readonly ulong wrapModulus = 1UL << (Math.Min(timestampSize, 4) * 8);
    private readonly DateTime?[] lastCycleUtc = new DateTime?[listCount];

    private bool anchored;
    private DateTime anchorHostUtc;
    private double anchorSlaveSeconds;
    private ulong unwrappedHigh;
    private uint lastRaw;
    private double correctionSeconds;
    private DateTime lastReceivedUtc;
    private DateTime lastEmittedUtc = DateTime.MinValue;
    private long reanchors;
    private bool driftReported;

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

        var mapped = anchorHostUtc.AddSeconds(slaveSeconds - anchorSlaveSeconds - correctionSeconds);
        var errorSeconds = (mapped - receivedUtc).TotalSeconds;

        if (Math.Abs(errorSeconds) > ReanchorThresholdSeconds)
        {
            reanchors++;
            logger?.Log(LogLevel.Debug,
                $"XCP: DAQ timestamps re-anchored to the receive time ({reanchors}× so far) — " +
                "slave restart or a transport stall shifted the clocks by more than " +
                $"{ReanchorThresholdSeconds:0.#} s.");
            Anchor(slaveSeconds, receivedUtc);
            mapped = receivedUtc;
        }
        else
        {
            // Bounded slew towards the receive time: only the drift exceeding the dead band
            // is corrected, at most MaxSlewRate of the receive-time progress per sample.
            var excessSeconds = errorSeconds > DriftDeadBandSeconds ? errorSeconds - DriftDeadBandSeconds
                : errorSeconds < -DriftDeadBandSeconds ? errorSeconds + DriftDeadBandSeconds
                : 0.0;
            if (excessSeconds != 0.0)
            {
                var progressSeconds = Math.Clamp((receivedUtc - lastReceivedUtc).TotalSeconds, 0.0, 1.0);
                var slew = Math.Clamp(excessSeconds, -MaxSlewRate * progressSeconds, MaxSlewRate * progressSeconds);
                correctionSeconds += slew;
                mapped = mapped.AddSeconds(-slew);
                ReportSustainedDrift(receivedUtc);
            }
        }

        lastReceivedUtc = receivedUtc;

        // The emitted axis must never run backwards (chart and log consumers rely on it);
        // equal timestamps are legal and merge into the previous sample downstream.
        if (mapped < lastEmittedUtc)
        {
            mapped = lastEmittedUtc;
        }

        lastEmittedUtc = mapped;
        lastCycleUtc[listIndex] = mapped;
        return mapped;
    }

    private void Anchor(double slaveSeconds, DateTime hostUtc)
    {
        anchored = true;
        anchorSlaveSeconds = slaveSeconds;
        anchorHostUtc = hostUtc;
        lastReceivedUtc = hostUtc;
        correctionSeconds = 0.0;
    }

    private void ReportSustainedDrift(DateTime receivedUtc)
    {
        if (driftReported)
        {
            return;
        }

        var elapsedSeconds = (receivedUtc - anchorHostUtc).TotalSeconds;
        if (elapsedSeconds < 30.0)
        {
            return;
        }

        var rate = correctionSeconds / elapsedSeconds;
        if (Math.Abs(rate) < DriftReportRate)
        {
            return;
        }

        driftReported = true;
        logger?.Log(LogLevel.Warn,
            $"XCP: the slave DAQ clock runs {Math.Abs(rate) * 100:0.##} % {(rate > 0 ? "fast" : "slow")} " +
            "against the host time (drift is being compensated) — check the slave clock source and " +
            "its advertised timestamp resolution.");
    }
}
