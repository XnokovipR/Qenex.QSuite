namespace Qenex.QSuite.Controls.Control;

/// <summary>
/// Marks a control whose display consumes every produced sample (time/XY series, peak
/// detection), not just the current value. The workspace sample pump queues all samples of
/// such a control and replays them in order on its UI tick; controls without this interface
/// receive only the latest pending sample per tick (their displays throttle by RefreshTime
/// anyway, intermediate values carry no information for them).
/// </summary>
public interface ISampleHistoryControl;
