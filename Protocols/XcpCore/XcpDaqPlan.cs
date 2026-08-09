namespace Qenex.QSuite.Protocols.XcpCore;

/// <summary>One ODT entry: a contiguous piece of ECU memory sampled into a DAQ packet (or
/// written from a STIM packet).</summary>
public sealed record XcpDaqEntryPlan(byte Size, byte AddressExtension, uint Address);

/// <summary>
/// One DAQ list bound to one ECU event channel, with its entries already packed into ODTs
/// (the packing respects MAX_DTO and MAX_ODT_ENTRY_SIZE and is the planner's job — the master
/// only replays the structure to the slave via ALLOC_* and WRITE_DAQ). <paramref name="IsStim"/>
/// selects the transfer direction (S1): false = DAQ (slave sends), true = STIM (master sends);
/// the structure is identical, only SET_DAQ_LIST_MODE gets the DIRECTION bit.
/// </summary>
public sealed record XcpDaqListPlan(ushort EventChannel, IReadOnlyList<IReadOnlyList<XcpDaqEntryPlan>> Odts,
    bool IsStim = false);
