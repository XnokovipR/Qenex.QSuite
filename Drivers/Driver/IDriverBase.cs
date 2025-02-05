using System.Text.Json.Serialization;
using Qenex.QSuite.ComponentSpecification;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Protocol;
using Qenex.QSuite.Specification;

namespace Qenex.QSuite.Drivers;

public interface IDriverBase : ICoreCommunication, IComponentSpecification
{
    #region Properties

    int Id { get; set; }
    string Label { get; set; }
    IList<IProtocolBase> Protocols { get; init; }

    #endregion

    #region Configuration

    void SetConfiguration(string rawSettings, string rawEncryptedSettings);
    void AddProtocol(IProtocolBase protocol);
    void AddProtocols(IEnumerable<IProtocolBase> protocols);
    void RemoveProtocol(IProtocolBase protocol);
    void RemoveProtocol(string protocolName);

    #endregion

    #region Send / Receive data
    void Send<T>(T data);
    Task SendAsync<T>(T data, CancellationToken ct = default);
    event EventHandler? OnDataReceived; 

    #endregion
    
}