using Qenex.QSuite.Controls.Control;

namespace Qenex.QInsight.EventAggregatorMsgs;

/// <summary>
/// Published by a workspace when the control selection in its diagram changes.
/// Control is null when the selection was cleared.
/// </summary>
public class WorkspaceControlSelectedMsg
{
    public string WorkspaceName { get; set; } = string.Empty;
    public ControlBase? Control { get; set; }
}
