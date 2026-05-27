using System.Reflection;
using MessagePack;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.FileDataReplayDriver;

public class FileDataReplayDriver : DriverBase, IReplayDriver
{
    private string logFilePath = Path.Combine(AppContext.BaseDirectory, "DataLogs", "values.qilog");
    private bool loop;
    private double speed = 1.0;
    private ReplayMode replayMode = ReplayMode.Realtime;
    private CancellationTokenSource? replayCancellation;
    private CancellationTokenSource? dataLoadCancellation;
    private Task? dataLoadTask;
    private Task? replayTask;
    private readonly object replayStateLock = new();
    private readonly SemaphoreSlim replayRecordGate = new(1, 1);
    private readonly SemaphoreSlim replayRecordsAvailable = new(0);
    private List<DataLogRecord> replayRecords = [];
    private int replayRecordIndex;
    private long replayFirstTimestampUtcTicks;
    private long replayCurrentTimestampUtcTicks;
    private int replaySeekVersion;
    private bool isPaused;
    private bool isDataLoaded;
    private bool isDataLoading;

    public FileDataReplayDriver()
    {
        Specification = new SpecificationBase
        {
            Name = "FileDataReplayDriver",
            Label = "File Data Replay Driver",
            Description = "Driver for replaying logged protocol variable values from a file.",
            CreatedOn = new DateTime(2026, 5, 25),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public event EventHandler? ReplayCompleted;
    public event EventHandler<ReplayProgressChangedEventArgs>? ReplayProgressChanged;

    public TimeSpan CurrentTime { get; private set; }
    public TimeSpan Duration { get; private set; }

    public bool IsDataLoaded
    {
        get
        {
            lock (replayStateLock)
            {
                return isDataLoaded;
            }
        }
    }

    public bool IsPaused
    {
        get
        {
            lock (replayStateLock)
            {
                return isPaused;
            }
        }
    }

    public override void SetConfiguration()
    {
        var settings = ParseSettings(RawSettings);
        if (settings.TryGetValue("file", out var configuredFile) && !string.IsNullOrWhiteSpace(configuredFile))
        {
            logFilePath = configuredFile;
        }

        if (settings.TryGetValue("directory", out var directory) && !string.IsNullOrWhiteSpace(directory))
        {
            var fileName = Path.GetFileName(logFilePath);
            logFilePath = Path.Combine(directory, fileName);
        }

        if (!Path.IsPathRooted(logFilePath))
        {
            logFilePath = Path.Combine(AppContext.BaseDirectory, logFilePath);
        }

        if (settings.TryGetValue("loop", out var loopValue) && bool.TryParse(loopValue, out var parsedLoop))
        {
            loop = parsedLoop;
        }

        if (settings.TryGetValue("speed", out var speedValue)
            && double.TryParse(speedValue, out var parsedSpeed)
            && parsedSpeed > 0)
        {
            speed = parsedSpeed;
        }

        if (settings.TryGetValue("mode", out var modeValue)
            && Enum.TryParse<ReplayMode>(modeValue, ignoreCase: true, out var parsedMode))
        {
            replayMode = parsedMode;
        }
    }

    public void StartLoadingData(CancellationToken ct = default)
    {
        lock (replayStateLock)
        {
            if (isDataLoaded || isDataLoading || dataLoadTask is { IsCompleted: false })
            {
                return;
            }

            isDataLoading = true;
        }

        dataLoadCancellation?.Dispose();
        dataLoadCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        dataLoadTask = LoadReplayRecordsSafeAsync(dataLoadCancellation.Token);
    }

    public override Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            return Task.CompletedTask;
        }

        foreach (var protocol in Protocols)
        {
            _ = protocol.StartAsync(ct);
        }

        StartLoadingData(ct);
        replayCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        replayTask = RunReplayAsync(replayCancellation.Token);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
        Resume();
        replayCancellation?.Cancel();
        if (replayTask != null)
        {
            try
            {
                await replayTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        foreach (var protocol in Protocols)
        {
            await protocol.StopAsync(ct);
        }

        replayCancellation?.Dispose();
        replayCancellation = null;
        replayTask = null;
    }

    public override void Dispose()
    {
        replayCancellation?.Cancel();
        replayCancellation?.Dispose();
        dataLoadCancellation?.Cancel();
        dataLoadCancellation?.Dispose();
        replayRecordGate.Dispose();
        replayRecordsAvailable.Dispose();
    }

    public override void Send<T>(T data)
    {
        throw new NotSupportedException("File data replay driver does not send command data.");
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        throw new NotSupportedException("File data replay driver does not send command data.");
    }

    public void Pause()
    {
        lock (replayStateLock)
        {
            if (!IsStarted || isPaused)
            {
                return;
            }

            isPaused = true;
        }

        RaiseReplayProgressChanged();
    }

    public void Resume()
    {
        lock (replayStateLock)
        {
            if (!isPaused)
            {
                return;
            }

            isPaused = false;
        }

        RaiseReplayProgressChanged();
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken ct = default)
    {
        if (!IsDataLoaded)
        {
            Logger?.Log(LogLevel.Warn, "Replay seek is available after the data log is fully loaded.");
            return;
        }

        if (position < TimeSpan.Zero)
        {
            position = TimeSpan.Zero;
        }

        if (position > Duration)
        {
            position = Duration;
        }

        List<DataLogRecord> recordsToReplay;
        lock (replayStateLock)
        {
            var targetTimestampUtcTicks = replayFirstTimestampUtcTicks + position.Ticks;
            replayRecordIndex = FindFirstRecordIndexAfter(targetTimestampUtcTicks);
            replayCurrentTimestampUtcTicks = targetTimestampUtcTicks;
            CurrentTime = position;
            replaySeekVersion++;
            recordsToReplay = GetRecordsUpTo(targetTimestampUtcTicks);
        }

        await replayRecordGate.WaitAsync(ct);
        try
        {
            await PublishRecordsAsync(recordsToReplay, ct);
        }
        finally
        {
            replayRecordGate.Release();
        }

        RaiseReplayProgressChanged();
    }

    private async Task RunReplayAsync(CancellationToken ct)
    {
        IsStarted = true;
        var completedNaturally = false;
        try
        {
            await Task.Yield();
            StartLoadingData(ct);

            do
            {
                await ReplayRecordsAsync(ct);
                if (loop && !ct.IsCancellationRequested)
                {
                    ResetReplayPosition();
                }
            }
            while (loop && !ct.IsCancellationRequested);

            completedNaturally = !ct.IsCancellationRequested;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Error, $"Replay failed: {e.Message}", e);
        }
        finally
        {
            IsStarted = false;
        }

        if (!completedNaturally)
        {
            return;
        }

        Logger?.Log(LogLevel.Info, "Replay reached end of data — raising ReplayCompleted.");
        try
        {
            ReplayCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Error, $"ReplayCompleted handler threw: {e.Message}", e);
        }
    }

    private async Task LoadReplayRecordsSafeAsync(CancellationToken ct)
    {
        try
        {
            await LoadReplayRecordsAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Error, $"Replay data load failed: {e.Message}", e);
            CompleteReplayDataLoad();
        }
    }

    private async Task LoadReplayRecordsAsync(CancellationToken ct)
    {
        if (!File.Exists(logFilePath))
        {
            Logger?.Log(LogLevel.Error, $"Replay log file \"{logFilePath}\" not found.");
            CompleteReplayDataLoad();
            return;
        }

        ResetReplayDataLoadState();

        await using var stream = new FileStream(
            logFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            useAsync: true);

        while (stream.Position < stream.Length)
        {
            ct.ThrowIfCancellationRequested();
            var serializedRecord = await MessagePackSerializer.DeserializeAsync<SerializedVariableLogRecord>(
                stream,
                cancellationToken: ct);
            AddLoadedRecord(serializedRecord.ToDataLogRecord());
        }

        CompleteReplayDataLoad();
    }

    private void ResetReplayDataLoadState()
    {
        lock (replayStateLock)
        {
            replayRecords = [];
            replayRecordIndex = 0;
            replaySeekVersion++;
            replayFirstTimestampUtcTicks = 0;
            replayCurrentTimestampUtcTicks = 0;
            CurrentTime = TimeSpan.Zero;
            Duration = TimeSpan.Zero;
            isDataLoaded = false;
            isDataLoading = true;
        }

        RaiseReplayProgressChanged();
    }

    private void AddLoadedRecord(DataLogRecord record)
    {
        lock (replayStateLock)
        {
            if (replayRecords.Count == 0)
            {
                replayFirstTimestampUtcTicks = GetRecordTimestampUtcTicks(record);
                replayCurrentTimestampUtcTicks = replayFirstTimestampUtcTicks;
            }

            replayRecords.Add(record);
            var recordTimestampUtcTicks = GetRecordTimestampUtcTicks(record);
            Duration = recordTimestampUtcTicks > replayFirstTimestampUtcTicks
                ? TimeSpan.FromTicks(recordTimestampUtcTicks - replayFirstTimestampUtcTicks)
                : Duration;
        }

        replayRecordsAvailable.Release();
    }

    private void CompleteReplayDataLoad()
    {
        lock (replayStateLock)
        {
            if (!IsStarted && replayRecordIndex == 0)
            {
                replayRecords = replayRecords
                    .OrderBy(GetRecordTimestampUtcTicks)
                    .ToList();
            }

            if (replayRecords.Count > 0)
            {
                replayFirstTimestampUtcTicks = GetRecordTimestampUtcTicks(replayRecords[0]);
                if (!IsStarted && replayRecordIndex == 0)
                {
                    replayCurrentTimestampUtcTicks = replayFirstTimestampUtcTicks;
                    CurrentTime = TimeSpan.Zero;
                }

                var lastTimestampUtcTicks = GetRecordTimestampUtcTicks(replayRecords[^1]);
                Duration = lastTimestampUtcTicks > replayFirstTimestampUtcTicks
                    ? TimeSpan.FromTicks(lastTimestampUtcTicks - replayFirstTimestampUtcTicks)
                    : TimeSpan.Zero;
            }

            isDataLoaded = true;
            isDataLoading = false;
        }

        replayRecordsAvailable.Release();
        RaiseReplayProgressChanged();
    }

    private async Task ReplayRecordsAsync(CancellationToken ct)
    {
        while (true)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            DataLogRecord? record;
            long currentTimestampUtcTicks;
            long recordTimestampUtcTicks;
            int seekVersion;
            lock (replayStateLock)
            {
                if (replayRecordIndex >= replayRecords.Count)
                {
                    if (isDataLoaded)
                    {
                        break;
                    }

                    record = null!;
                    currentTimestampUtcTicks = 0;
                    recordTimestampUtcTicks = 0;
                    seekVersion = replaySeekVersion;
                }
                else
                {
                    record = replayRecords[replayRecordIndex];
                    currentTimestampUtcTicks = replayCurrentTimestampUtcTicks;
                    recordTimestampUtcTicks = GetRecordTimestampUtcTicks(record);
                    seekVersion = replaySeekVersion;
                }
            }

            if (record == null)
            {
                if (!await WaitForReplayRecordsAvailableAsync(ct))
                {
                    break;
                }

                continue;
            }

            var canContinue = await DelayBeforeReplayAsync(currentTimestampUtcTicks, recordTimestampUtcTicks, seekVersion, ct);
            if (!canContinue)
            {
                continue;
            }

            if (!await WaitForReplayRecordGateAsync(ct))
            {
                break;
            }

            try
            {
                if (!IsCurrentSeekVersion(seekVersion))
                {
                    continue;
                }

                await PublishRecordAsync(record, ct);
                lock (replayStateLock)
                {
                    if (!IsCurrentSeekVersionCore(seekVersion))
                    {
                        continue;
                    }

                    replayRecordIndex++;
                    replayCurrentTimestampUtcTicks = recordTimestampUtcTicks;
                    CurrentTime = GetRelativeReplayTime(recordTimestampUtcTicks);
                }

                RaiseReplayProgressChanged();
            }
            finally
            {
                replayRecordGate.Release();
            }
        }
    }

    private async Task<bool> DelayBeforeReplayAsync(
        long previousTimestampUtcTicks,
        long currentTimestampUtcTicks,
        int seekVersion,
        CancellationToken ct)
    {
        if (replayMode == ReplayMode.Immediate)
        {
            return await WaitWhilePausedAsync(seekVersion, ct);
        }

        var ticks = currentTimestampUtcTicks - previousTimestampUtcTicks;
        if (ticks <= 0)
        {
            return await WaitWhilePausedAsync(seekVersion, ct);
        }

        var remainingDelay = TimeSpan.FromTicks((long)(ticks / speed));
        while (remainingDelay > TimeSpan.Zero)
        {
            if (!await WaitWhilePausedAsync(seekVersion, ct))
            {
                return false;
            }

            if (!IsCurrentSeekVersion(seekVersion))
            {
                return false;
            }

            var delay = remainingDelay > TimeSpan.FromMilliseconds(50)
                ? TimeSpan.FromMilliseconds(50)
                : remainingDelay;
            await Task.Delay(delay);
            remainingDelay -= delay;
        }

        return !ct.IsCancellationRequested && IsCurrentSeekVersion(seekVersion);
    }

    private async Task<bool> WaitWhilePausedAsync(int seekVersion, CancellationToken ct)
    {
        while (true)
        {
            if (ct.IsCancellationRequested)
            {
                return false;
            }

            if (!IsCurrentSeekVersion(seekVersion))
            {
                return false;
            }

            if (!IsPaused)
            {
                return true;
            }

            await Task.Delay(50);
        }
    }

    private async Task<bool> WaitForReplayRecordsAvailableAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (await replayRecordsAvailable.WaitAsync(TimeSpan.FromMilliseconds(50)))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> WaitForReplayRecordGateAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (await replayRecordGate.WaitAsync(TimeSpan.FromMilliseconds(50)))
            {
                return true;
            }
        }

        return false;
    }

    private async Task PublishRecordAsync(DataLogRecord record, CancellationToken ct)
    {
        await PublishRecordsAsync([record], ct);
    }

    private async Task PublishRecordsAsync(IEnumerable<DataLogRecord> records, CancellationToken ct)
    {
        foreach (var protocol in Protocols.OfType<ProtocolBase<DataLogRecord>>())
        {
            await protocol.AddReceivedDataToQueueAsync(records, ct);
        }
    }

    private void ResetReplayPosition()
    {
        lock (replayStateLock)
        {
            replayRecordIndex = 0;
            replayCurrentTimestampUtcTicks = replayFirstTimestampUtcTicks;
            CurrentTime = TimeSpan.Zero;
            replaySeekVersion++;
        }

        RaiseReplayProgressChanged();
    }

    private int FindFirstRecordIndexAfter(long timestampUtcTicks)
    {
        var low = 0;
        var high = replayRecords.Count;
        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (GetRecordTimestampUtcTicks(replayRecords[mid]) <= timestampUtcTicks)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    private List<DataLogRecord> GetRecordsUpTo(long timestampUtcTicks)
    {
        var records = new List<DataLogRecord>();
        foreach (var record in replayRecords)
        {
            if (GetRecordTimestampUtcTicks(record) > timestampUtcTicks)
            {
                break;
            }

            records.Add(record);
        }

        return records;
    }

    private TimeSpan GetRelativeReplayTime(long timestampUtcTicks)
    {
        if (replayFirstTimestampUtcTicks == 0 || timestampUtcTicks <= replayFirstTimestampUtcTicks)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromTicks(timestampUtcTicks - replayFirstTimestampUtcTicks);
    }

    private bool IsCurrentSeekVersion(int seekVersion)
    {
        lock (replayStateLock)
        {
            return IsCurrentSeekVersionCore(seekVersion);
        }
    }

    private bool IsCurrentSeekVersionCore(int seekVersion)
    {
        return replaySeekVersion == seekVersion;
    }

    private void RaiseReplayProgressChanged()
    {
        ReplayProgressChanged?.Invoke(
            this,
            new ReplayProgressChangedEventArgs(CurrentTime, Duration, IsPaused, IsDataLoaded));
    }

    private static long GetRecordTimestampUtcTicks(DataLogRecord record)
    {
        return record.TimestampUtcTicks > 0 ? record.TimestampUtcTicks : 0;
    }

    private static Dictionary<string, string> ParseSettings(string rawSettings)
    {
        return rawSettings
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(
                parts => parts[0],
                parts => parts[1].Trim('"'),
                StringComparer.OrdinalIgnoreCase);
    }

    private enum ReplayMode
    {
        Realtime,
        Immediate
    }
}
