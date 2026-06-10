namespace Qenex.QSuite.Common.CoreComm;

public sealed class CommunicationStateChangedEventArgs(
    CommunicationState previousState,
    CommunicationState currentState,
    string? message) : EventArgs
{
    public CommunicationState PreviousState { get; } = previousState;
    public CommunicationState CurrentState { get; } = currentState;
    public string? Message { get; } = message;
}
