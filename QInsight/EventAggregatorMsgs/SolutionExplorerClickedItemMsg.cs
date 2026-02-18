using Qenex.QInsight.ViewModels.ViewableItem;

namespace Qenex.QInsight.EventAggregatorMsgs;

public class SolutionExplorerClickedItemMsg
{
    public IViewableItem Item { get; set; } = null!;
}