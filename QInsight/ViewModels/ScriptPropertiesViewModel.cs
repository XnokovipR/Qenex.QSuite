using Qenex.QInsight.ViewModels;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripts.Script;

namespace Qenex.QInsight.ViewModels;

public class ScriptPropertiesViewModel(EventAggregator ea, IScriptBase s) : IPropertiesViewModel
{
    private IScriptBase script = s;
}