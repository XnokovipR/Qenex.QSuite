using Qenex.QSuite.CoreComm;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Protocol;

public abstract class ProtocolBase : IProtocolBase
{
    public ISpecification Specification { get; }
    
    public bool IsEnabled { get; set; }
    public bool IsStarted { get; set; }
    
    public abstract Task StartAsync();
    public abstract Task StopAsync();
    public abstract void Dispose();
}