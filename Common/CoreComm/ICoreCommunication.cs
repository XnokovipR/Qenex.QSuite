namespace Qenex.QSuite.CoreComm;

public interface ICoreCommunication : IDisposable
{
    bool IsEnabled { get; set; }
    bool IsStarted { get; set; }
    
    Task StartAsync();
    Task StopAsync();
}