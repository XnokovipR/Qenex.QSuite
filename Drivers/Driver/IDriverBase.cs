using System.Text.Json.Serialization;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.CoreComm;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Driver;

public interface IDriverBase : ICoreCommunication, IComponentSpecification
{
    public IList<IProtocolBase> Protocols { get; init; }
    
    public void AddProtocol(IProtocolBase protocol);
    public void RemoveProtocol(IProtocolBase protocol);
    public void RemoveProtocol(string protocolName);
    
    public void Send<T>(T data);
    public Task SendAsync<T>(T data, CancellationToken ct = default);
    
    public event EventHandler? OnDataReceived; 
}