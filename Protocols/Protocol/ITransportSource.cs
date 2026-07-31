namespace Qenex.QSuite.Protocols.Protocol;

/// <summary>
/// Declares the transport payload type a driver exchanges with the protocols it hosts
/// (e.g. <c>CanFrame</c> for a CAN adapter, <c>byte[]</c> for serial/TCP byte streams).
/// Purely declarative — it carries no members; the Project Configurator matches it against
/// the <see cref="ProtocolBase{T}"/> frame type to offer only compatible protocols.
/// Implement it once per supported payload type; a driver without it is treated as
/// unrestricted and no filtering is applied.
/// </summary>
public interface ITransportSource<T>;
