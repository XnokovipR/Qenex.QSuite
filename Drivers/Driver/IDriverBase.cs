using System.Text.Json.Serialization;
using Qenex.QSuite.Specifications.ComponentSpecification;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.Driver;

public interface IDriverBase : ICoreCommunication, IComponentSpecification
{
    #region Properties

    int Id { get; set; }
    string Label { get; set; }
    string RawSettings { get; set; }
    string RawEncryptedSettings { get; set; }

    /// <summary>
    /// Settings template pre-filled when the driver is added in Project Configuration: it lists
    /// every parameter with a representative value so the operator can see exactly what can be
    /// configured and only edits the values (empty means the driver has no settings).
    /// </summary>
    string DefaultRawSettings { get; }

    IList<IProtocolBase> Protocols { get; init; }

    #endregion

    #region Configuration

    void SetConfiguration();
    void AddProtocol(IProtocolBase protocol);
    void AddProtocols(IEnumerable<IProtocolBase> protocols);
    void RemoveProtocol(IProtocolBase protocol);
    void RemoveProtocol(string protocolName);

    #endregion

    #region Send / Receive data
    void Send<T>(T data);
    Task SendAsync<T>(T data, CancellationToken ct = default);
    //event EventHandler? OnDriverDataReceived;

    #endregion
    
}
