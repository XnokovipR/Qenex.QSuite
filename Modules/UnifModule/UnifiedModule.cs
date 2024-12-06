using System.Reflection;
using Qenex.QSuite.Driver;
using Qenex.QSuite.LogSystem;
using Qenex.QSuite.Module;
using Qenex.QSuite.Specification;
using Qenex.QSuite.QVariables;

namespace Qenex.QSuite.UnifModule;

/// <summary>
/// Unified model is common for both QSuiteModel and also for Model working as a independent module executed in service.
/// </summary>
public class UnifiedModule : ModuleBase
{
    public UnifiedModule()
    {
        Specification = new SpecificationBase()
        {
            Name = "UnifiedModule",
        };
    }
}