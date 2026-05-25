using System.Reflection;
using MessagePack;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.FileDataReplayDriver;

public class FileDataReplayDriver : DriverBase, IReplayDriver
{
    private string logFilePath = Path.Combine(AppContext.BaseDirectory, "DataLogs", "values.msgpack");
    private bool loop;
    private double speed = 1.0;
    private ReplayMode replayMode = ReplayMode.Realtime;
    private CancellationTokenSource? replayCancellation;
    private Task? replayTask;

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

        replayCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        replayTask = RunReplayAsync(replayCancellation.Token);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
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
    }

    public override void Send<T>(T data)
    {
        throw new NotSupportedException("File data replay driver does not send command data.");
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        throw new NotSupportedException("File data replay driver does not send command data.");
    }

    private async Task RunReplayAsync(CancellationToken ct)
    {
        IsStarted = true;
        var completedNaturally = false;
        try
        {
            await Task.Yield();

            do
            {
                await ReplayFileAsync(ct);
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

    private async Task ReplayFileAsync(CancellationToken ct)
    {
        if (!File.Exists(logFilePath))
        {
            Logger?.Log(LogLevel.Error, $"Replay log file \"{logFilePath}\" not found.");
            return;
        }

        await using var stream = new FileStream(
            logFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            useAsync: true);

        DataLogRecord? previousRecord = null;
        while (stream.Position < stream.Length)
        {
            ct.ThrowIfCancellationRequested();
            var serializedRecord = await MessagePackSerializer.DeserializeAsync<SerializedVariableLogRecord>(
                stream,
                cancellationToken: ct);
            var record = serializedRecord.ToDataLogRecord();

            if (previousRecord != null)
            {
                await DelayBeforeReplayAsync(previousRecord, record, ct);
            }

            await ReplayRecordAsync(record, ct);
            previousRecord = record;
        }
    }

    private async Task DelayBeforeReplayAsync(DataLogRecord previousRecord, DataLogRecord currentRecord, CancellationToken ct)
    {
        if (replayMode == ReplayMode.Immediate)
        {
            return;
        }

        var ticks = currentRecord.TimestampUtcTicks - previousRecord.TimestampUtcTicks;
        if (ticks <= 0)
        {
            return;
        }

        var delay = TimeSpan.FromTicks((long)(ticks / speed));
        await Task.Delay(delay, ct);
    }

    private async Task ReplayRecordAsync(DataLogRecord record, CancellationToken ct)
    {
        foreach (var protocol in Protocols.OfType<ProtocolBase<DataLogRecord>>())
        {
            await protocol.AddReceivedDataToQueueAsync([record], ct);
        }
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
