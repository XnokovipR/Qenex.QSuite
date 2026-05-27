namespace Qenex.QSuite.Drivers.Driver;

public interface IReplayDriver
{
    event EventHandler? ReplayCompleted;
    event EventHandler<ReplayProgressChangedEventArgs>? ReplayProgressChanged;

    TimeSpan CurrentTime { get; }
    TimeSpan Duration { get; }
    bool IsPaused { get; }
    bool IsDataLoaded { get; }

    void StartLoadingData(CancellationToken ct = default);
    void Pause();
    void Resume();
    Task SeekAsync(TimeSpan position, CancellationToken ct = default);
}

public sealed class ReplayProgressChangedEventArgs : EventArgs
{
    public ReplayProgressChangedEventArgs(TimeSpan currentTime, TimeSpan duration, bool isPaused, bool isDataLoaded)
    {
        CurrentTime = currentTime;
        Duration = duration;
        IsPaused = isPaused;
        IsDataLoaded = isDataLoaded;
    }

    public TimeSpan CurrentTime { get; }
    public TimeSpan Duration { get; }
    public bool IsPaused { get; }
    public bool IsDataLoaded { get; }
}
