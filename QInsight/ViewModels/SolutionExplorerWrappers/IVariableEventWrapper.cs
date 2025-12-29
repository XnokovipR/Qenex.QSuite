using Qenex.QInsight.ViewModels.ViewableItem;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QInsight.ViewModels.SolutionExplorerWrappers;

public interface IVariableEventWrapper : IViewableItem
{
    public IVarEvent VariableEvent { get; init; }
}