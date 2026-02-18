using Qenex.QInsight.ViewModels.ViewableItem;

namespace Qenex.QInsight.EventAggregatorMsgs;

public class SolutionExplorerDoubleClickedItemMsg
{
    public IViewableItem Item { get; set; } = null!;
}