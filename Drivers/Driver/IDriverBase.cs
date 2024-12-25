using System.Text.Json.Serialization;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Driver;

public interface IDriverBase : ICoreCommunication, IComponentSpecification
{
    int Id { get; set; }
    string Label { get; set; }
    IList<IProtocolBase> Protocols { get; init; }
    
    void SetConfiguration(string rawSettings, string rawEncryptedSettings);
    void AddProtocol(IProtocolBase protocol);
    void AddProtocols(IEnumerable<IProtocolBase> protocols);
    void RemoveProtocol(IProtocolBase protocol);
    void RemoveProtocol(string protocolName);
    
    void Send<T>(T data);
    Task SendAsync<T>(T data, CancellationToken ct = default);
    
    event EventHandler? OnDataReceived; 
}