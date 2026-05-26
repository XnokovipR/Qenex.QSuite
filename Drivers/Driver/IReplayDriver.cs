namespace Qenex.QSuite.Drivers.Driver;

public interface IReplayDriver
{
    event EventHandler? ReplayCompleted;
    event EventHandler<ReplayProgressChangedEventArgs>? ReplayProgressChanged;

    TimeSpan CurrentTime { get; }
    TimeSpan Duration { get; }
    bool IsPaused { get; }

    void Pause();
    void Resume();
    Task SeekAsync(TimeSpan position, CancellationToken ct = default);
}

public sealed class ReplayProgressChangedEventArgs : EventArgs
{
    public ReplayProgressChangedEventArgs(TimeSpan currentTime, TimeSpan duration, bool isPaused)
    {
        CurrentTime = currentTime;
        Duration = duration;
        IsPaused = isPaused;
    }

    public TimeSpan CurrentTime { get; }
    public TimeSpan Duration { get; }
    public bool IsPaused { get; }
}
