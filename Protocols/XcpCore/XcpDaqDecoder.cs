using System.Buffers.Binary;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>
/// Decodes received DAQ DTO packets against the configured list structure: resolves the
/// identification field (per the slave's DAQ_KEY_BYTE type), skips the ODT-0 timestamp when the
/// session runs with timestamps, and yields one slice per ODT entry in configuration order.
/// Malformed or unmappable packets are dropped with a throttled warning — a live DAQ stream must
/// never spam the log.
/// </summary>
public sealed class XcpDaqDecoder(
    XcpDaqIdentificationType identificationType,
    bool isBigEndian,
    int timestampSizeOdt0,
    bool overloadIndicationByPid,
    IReadOnlyList<XcpDaqListPlan> lists,
    ILogger? logger = null)
{
    private const int DropLogInterval = 1000;

    /// <summary>One decoded ODT entry: which configured entry it is and where its bytes sit in
    /// the packet.</summary>
    public readonly record struct DecodedEntry(int ListIndex, int OdtIndex, int EntryIndex, int DataOffset, byte Size);

    // Absolute-PID slaves number all ODTs of all lists consecutively; FIRST_PID per list comes
    // from the START_STOP_DAQ_LIST responses and is injected after configuration.
    private byte[]? firstPids;

    private long droppedPackets;
    private long overloadFlags;

    /// <summary>Required for slaves with absolute-PID identification before decoding starts.</summary>
    public void SetFirstPids(byte[] pids)
    {
        firstPids = pids;
    }

    /// <summary>Count of packets dropped as malformed/unmappable so far.</summary>
    public long DroppedPackets => droppedPackets;

    /// <summary>
    /// Decodes one DTO into <paramref name="entries"/> (cleared first). Returns false when the
    /// packet had to be dropped. The caller slices the packet by the returned offsets.
    /// </summary>
    public bool TryDecode(byte[] packet, List<DecodedEntry> entries)
    {
        entries.Clear();

        if (!TryResolveHeader(packet, out var listIndex, out var odtIndex, out var headerSize))
        {
            return false;
        }

        var offset = headerSize + (odtIndex == 0 ? timestampSizeOdt0 : 0);
        var odtEntries = lists[listIndex].Odts[odtIndex];

        for (var entryIndex = 0; entryIndex < odtEntries.Count; entryIndex++)
        {
            var size = odtEntries[entryIndex].Size;
            if (offset + size > packet.Length)
            {
                return Drop($"DTO for DAQ list {listIndex} ODT {odtIndex} carries {packet.Length} bytes, " +
                            $"entry {entryIndex} needs bytes up to {offset + size}");
            }

            entries.Add(new DecodedEntry(listIndex, odtIndex, entryIndex, offset, size));
            offset += size;
        }

        return true;
    }

    private bool TryResolveHeader(byte[] packet, out int listIndex, out int odtIndex, out int headerSize)
    {
        listIndex = -1;
        odtIndex = -1;
        headerSize = identificationType switch
        {
            XcpDaqIdentificationType.AbsolutePid => 1,
            XcpDaqIdentificationType.OdtWithDaqByte => 2,
            XcpDaqIdentificationType.OdtWithDaqWord => 3,
            _ => 4
        };

        if (packet.Length < headerSize)
        {
            return Drop($"DTO shorter than its {headerSize}-byte identification field ({packet.Length} bytes)");
        }

        if (identificationType == XcpDaqIdentificationType.AbsolutePid)
        {
            return TryResolveAbsolutePid(packet[0], out listIndex, out odtIndex) ||
                   Drop($"DTO PID 0x{packet[0]:X2} does not fall into any configured DAQ list");
        }

        var odtByte = packet[0];
        if (overloadIndicationByPid && (odtByte & 0x80) != 0)
        {
            odtByte &= 0x7F;
            NoteOverload();
        }

        var daq = identificationType switch
        {
            XcpDaqIdentificationType.OdtWithDaqByte => packet[1],
            XcpDaqIdentificationType.OdtWithDaqWord => ReadUInt16(packet.AsSpan(1, 2)),
            _ => ReadUInt16(packet.AsSpan(2, 2))
        };

        if (daq >= lists.Count)
        {
            return Drop($"DTO names DAQ list {daq}, only {lists.Count} lists are configured");
        }

        if (odtByte >= lists[daq].Odts.Count)
        {
            return Drop($"DTO names ODT {odtByte} of DAQ list {daq}, which has only {lists[daq].Odts.Count} ODTs");
        }

        listIndex = daq;
        odtIndex = odtByte;
        return true;
    }

    private bool TryResolveAbsolutePid(byte pid, out int listIndex, out int odtIndex)
    {
        listIndex = -1;
        odtIndex = -1;
        if (firstPids == null)
        {
            return false;
        }

        if (TryMapAbsolutePid(pid, out listIndex, out odtIndex))
        {
            return true;
        }

        // OVERLOAD_INDICATION_PID sets the MSB of the PID; retry without it before giving up.
        if (overloadIndicationByPid && (pid & 0x80) != 0 && TryMapAbsolutePid((byte)(pid & 0x7F), out listIndex, out odtIndex))
        {
            NoteOverload();
            return true;
        }

        return false;
    }

    private bool TryMapAbsolutePid(byte pid, out int listIndex, out int odtIndex)
    {
        for (var daq = 0; daq < lists.Count; daq++)
        {
            var first = firstPids![daq];
            if (pid >= first && pid < first + lists[daq].Odts.Count)
            {
                listIndex = daq;
                odtIndex = pid - first;
                return true;
            }
        }

        listIndex = -1;
        odtIndex = -1;
        return false;
    }

    private void NoteOverload()
    {
        // D6: overload is a warning; log the first occurrence and then only every Nth.
        overloadFlags++;
        if (overloadFlags == 1 || overloadFlags % DropLogInterval == 0)
        {
            logger?.Log(LogLevel.Warn,
                $"XCP: DAQ overload indicated by the slave (PID flag, {overloadFlags}× so far) — samples were lost; the session continues.");
        }
    }

    private bool Drop(string reason)
    {
        droppedPackets++;
        if (droppedPackets == 1 || droppedPackets % DropLogInterval == 0)
        {
            logger?.Log(LogLevel.Warn, $"XCP: DAQ packet dropped ({reason}); {droppedPackets} dropped so far.");
        }

        return false;
    }

    private ushort ReadUInt16(ReadOnlySpan<byte> source)
    {
        return isBigEndian
            ? BinaryPrimitives.ReadUInt16BigEndian(source)
            : BinaryPrimitives.ReadUInt16LittleEndian(source);
    }
}
