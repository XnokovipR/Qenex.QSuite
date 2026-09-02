namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>
/// Packs DAQ/STIM entries into ODTs of one list. First-fit over all ODTs of the list (not only
/// the last one): an entry goes into the first ODT with enough room, so small entries can still
/// land in ODT 0 next to the timestamp when a big one already opened ODT 1. ODT 0 loses the
/// timestamp bytes; every other ODT offers MAX_DTO minus the identification field.
///
/// A slave (ours included) rejects an ODT without entries (ALLOC_ODT_ENTRY count 0), and the
/// timestamp lives in ODT 0 — so when nothing fits beside the timestamp (typical on classic CAN:
/// 8 B DTO, 2 B header, 4 B timestamp, 4 B variables) ODT 0 gets a <em>filler</em> entry: the
/// first bytes of the first real entry, sampled again and discarded by the consumer. It costs
/// no extra frame (ODT 0 is transmitted anyway) and keeps the list standard-conforming.
/// Fillers are only allowed for DAQ; a STIM filler would write ECU memory.
/// </summary>
public static class XcpOdtPacker
{
    /// <summary>What the caller wants packed; the index into this list identifies it in the result.</summary>
    public readonly record struct Item(byte Size, byte AddressExtension, uint Address);

    /// <summary>One packed ODT entry. <see cref="ItemIndex"/> is <see cref="FillerItem"/> for the
    /// ODT 0 timestamp-carrier filler, otherwise the index of the packed item.</summary>
    public readonly record struct Slot(int ItemIndex, XcpDaqEntryPlan Entry)
    {
        public bool IsFiller => ItemIndex == FillerItem;
    }

    public const int FillerItem = -1;

    /// <summary>Why an item was left out of the list.</summary>
    public enum RejectReason
    {
        /// <summary>Larger than MAX_ODT_ENTRY_SIZE or than a plain ODT payload.</summary>
        TooLarge,

        /// <summary>The list would need more ODTs than addressable.</summary>
        NoOdtLeft
    }

    public sealed record Result(
        IReadOnlyList<IReadOnlyList<Slot>> Odts,
        IReadOnlyList<(int ItemIndex, RejectReason Reason)> Rejected,
        bool Odt0HasFiller,
        bool Odt0Unfillable);

    /// <param name="items">Entries in configuration order.</param>
    /// <param name="maxDto">MAX_DTO of the slave.</param>
    /// <param name="headerSize">Identification field size (DTO header).</param>
    /// <param name="timestampSizeOdt0">Timestamp bytes carried in ODT 0, 0 when untimestamped.</param>
    /// <param name="maxEntrySize">MAX_ODT_ENTRY_SIZE (DAQ or STIM side).</param>
    /// <param name="maxOdts">Addressable ODTs per list (252 for DAQ, 192 for STIM).</param>
    /// <param name="allowFiller">Permit the ODT 0 filler entry (DAQ only).</param>
    public static Result Pack(IReadOnlyList<Item> items, int maxDto, int headerSize, int timestampSizeOdt0,
        int maxEntrySize, int maxOdts, bool allowFiller)
    {
        var plainCapacity = maxDto - headerSize;
        var odt0Capacity = plainCapacity - timestampSizeOdt0;

        var odts = new List<List<Slot>>();
        var fill = new List<int>();
        var rejected = new List<(int, RejectReason)>();

        int Capacity(int odtIndex) => odtIndex == 0 ? odt0Capacity : plainCapacity;

        for (var itemIndex = 0; itemIndex < items.Count; itemIndex++)
        {
            var item = items[itemIndex];
            var size = item.Size;
            if (size == 0 || size > maxEntrySize || size > plainCapacity)
            {
                rejected.Add((itemIndex, RejectReason.TooLarge));
                continue;
            }

            var odtIndex = -1;
            for (var i = 0; i < odts.Count; i++)
            {
                if (fill[i] + size <= Capacity(i) && odts[i].Count < byte.MaxValue)
                {
                    odtIndex = i;
                    break;
                }
            }

            if (odtIndex < 0)
            {
                // Open a new ODT. ODT 0 may be too small for this entry (timestamp) — it is then
                // opened empty and (if nothing smaller comes later) gets the filler below.
                if (odts.Count == 0 && size > odt0Capacity)
                {
                    odts.Add([]);
                    fill.Add(0);
                }

                if (odts.Count >= maxOdts)
                {
                    rejected.Add((itemIndex, RejectReason.NoOdtLeft));
                    continue;
                }

                odts.Add([]);
                fill.Add(0);
                odtIndex = odts.Count - 1;
            }

            odts[odtIndex].Add(new Slot(itemIndex, new XcpDaqEntryPlan(size, item.AddressExtension, item.Address)));
            fill[odtIndex] += size;
        }

        var odt0HasFiller = false;
        var odt0Unfillable = false;
        if (odts.Count > 0 && odts[0].Count == 0)
        {
            var fillerSize = Math.Min(odt0Capacity, maxEntrySize);
            if (allowFiller && fillerSize >= 1 && odts.Count > 1 && odts[1].Count > 0)
            {
                var carrier = odts[1][0].Entry;
                odts[0].Add(new Slot(FillerItem, new XcpDaqEntryPlan((byte)fillerSize, carrier.AddressExtension, carrier.Address)));
                odt0HasFiller = true;
            }
            else
            {
                odt0Unfillable = true;
                odts.Clear();
            }
        }

        return new Result(
            odts.Select(IReadOnlyList<Slot> (o) => o).ToList(),
            rejected,
            odt0HasFiller,
            odt0Unfillable);
    }
}
