namespace Qenex.QSuite.Protocols.Protocol;

/// <summary>
/// A protocol that transmits frames of type <typeparamref name="T"/> through the driver hosting it
/// (e.g. an XCP master sending request frames). The driver injects a transmitter delegate while its
/// connection is usable — right before starting its protocols — and clears it (null) on stop. The
/// protocol stays free of any driver dependency, and a transport swap (CAN today, TCP later) only
/// changes which driver injects the delegate.
/// </summary>
public interface ITransportProtocol<T>
{
    void SetTransmitter(Func<T, CancellationToken, Task>? transmitter);
}
