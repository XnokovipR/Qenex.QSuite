using Qenex.QInsight.ViewModels.ViewableItem;

namespace Qenex.QInsight.EventAggregatorMsgs;

public class SolutionExplorerItemMsg
{
    public IViewableItem Item { get; set; } = null!;
}