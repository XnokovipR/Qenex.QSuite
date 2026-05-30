using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public interface IVariableEventSeWrapper : IViewableItem
{
    public IVarEvent VariableEvent { get; init; }
    public void Refresh();
}
