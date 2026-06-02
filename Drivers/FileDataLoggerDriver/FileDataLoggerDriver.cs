using System.Globalization;
using System.Reflection;
using MessagePack;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.FileDataLoggerDriver;

public class FileDataLoggerDriver : DriverBase, IProtocolVariableSinkDriver, IDataLogFileNameDriver
{
    private const string DataLogExtension = ".qilog";
    private const string TimestampFormat = "yyyyMMdd'_'HH'h'mm";
    private static readonly TimeSpan DefaultReorderBufferDelay = TimeSpan.FromMilliseconds(500);
    private readonly object pendingRecordsLock = new();
    private readonly LinkedList<BufferedVariableLogRecord> pendingRecords = [];
    private readonly EventWaitHandle waitHandle = new AutoResetEvent(false);
    private string logFilePath = Path.Combine(AppContext.BaseDirectory, "DataLogs");
    private bool append = true;
    private bool flushOnWrite;
    private TimeSpan reorderBufferDelay = DefaultReorderBufferDelay;
    private long highestBufferedTimestampUtcTicks;
    private volatile bool exitRequested;
    private FileStream? logStream;
    private Task? writerTask;

    public FileDataLoggerDriver()
    {
        Specification = new SpecificationBase
        {
            Name = "FileDataLoggerDriver",
            Label = "File Data Logger Driver",
            Description = "Driver for logging observed protocol variable values into a file.",
            CreatedOn = new DateTime(2026, 5, 25),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public string? DataLogFileName { get; set; }

    public override void SetConfiguration()
    {
        logFilePath = Path.Combine(AppContext.BaseDirectory, "DataLogs");
        append = true;
        flushOnWrite = false;
        reorderBufferDelay = DefaultReorderBufferDelay;

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

        if (settings.TryGetValue("append", out var appendValue) && bool.TryParse(appendValue, out var parsedAppend))
        {
            append = parsedAppend;
        }

        if (settings.TryGetValue("flushOnWrite", out var flushValue) && bool.TryParse(flushValue, out var parsedFlush))
        {
            flushOnWrite = parsedFlush;
        }

        if (settings.TryGetValue("reorderBufferMs", out var reorderBufferValue)
            && double.TryParse(reorderBufferValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedReorderBufferMs)
            && parsedReorderBufferMs >= 0)
        {
            reorderBufferDelay = TimeSpan.FromMilliseconds(parsedReorderBufferMs);
        }
    }

    public bool CanSubscribe(IProtocolVariable sourceVariable)
    {
        return Protocols
            .OfType<IProtocolVariableSinkProtocol>()
            .Any(protocol => protocol.CanProcess(sourceVariable));
    }

    public Task OnProtocolVariableValueChangedAsync(IProtocolVariable sourceVariable)
    {
        if (!IsStarted || logStream == null || !CanSubscribe(sourceVariable))
        {
            return Task.CompletedTask;
        }

        var record = CreateRecord(sourceVariable);
        BufferRecord(record);
        waitHandle.Set();
        return Task.CompletedTask;
    }

    public override Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled) return Task.CompletedTask;
        if (IsStarted) return Task.CompletedTask;

        var timestampedLogFilePath = CreateTimestampedLogFilePath(logFilePath, DataLogFileName, DateTime.Now);
        var directory = Path.GetDirectoryName(timestampedLogFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        logStream = new FileStream(
            timestampedLogFilePath,
            append ? FileMode.Append : FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        exitRequested = false;
        highestBufferedTimestampUtcTicks = 0;
        writerTask = RunWriterLoopAsync();
        IsStarted = true;
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
        exitRequested = true;
        waitHandle.Set();

        if (writerTask != null)
        {
            await writerTask.WaitAsync(ct);
            writerTask = null;
        }

        if (logStream != null)
        {
            await logStream.FlushAsync(ct);
            await logStream.DisposeAsync();
            logStream = null;
        }

        IsStarted = false;
    }

    public override void Dispose()
    {
        exitRequested = true;
        waitHandle.Set();
        writerTask?.Wait(TimeSpan.FromSeconds(1));
        logStream?.Dispose();
        waitHandle.Dispose();
    }

    public override void Send<T>(T data)
    {
        throw new NotSupportedException("File data logger driver does not send data.");
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        throw new NotSupportedException("File data logger driver does not send data.");
    }

    private async Task RunWriterLoopAsync()
    {
        await Task.Run(async () =>
        {
            while (!exitRequested || HasPendingRecords())
            {
                var record = TryTakeWritableRecord();
                if (record != null)
                {
                    await WriteRecordAsync(record);
                }
                else
                {
                    waitHandle.WaitOne(TimeSpan.FromMilliseconds(20));
                }
            }
        });
    }

    private void BufferRecord(VariableLogRecord record)
    {
        var bufferedRecord = new BufferedVariableLogRecord(record);
        lock (pendingRecordsLock)
        {
            if (record.TimestampUtcTicks > highestBufferedTimestampUtcTicks)
            {
                highestBufferedTimestampUtcTicks = record.TimestampUtcTicks;
            }

            if (pendingRecords.Last == null)
            {
                pendingRecords.AddLast(bufferedRecord);
                return;
            }

            var node = pendingRecords.Last;
            while (node != null && node.Value.Record.TimestampUtcTicks > record.TimestampUtcTicks)
            {
                node = node.Previous;
            }

            if (node == null)
            {
                pendingRecords.AddFirst(bufferedRecord);
            }
            else
            {
                pendingRecords.AddAfter(node, bufferedRecord);
            }
        }
    }

    private bool HasPendingRecords()
    {
        lock (pendingRecordsLock)
        {
            return pendingRecords.Count > 0;
        }
    }

    private VariableLogRecord? TryTakeWritableRecord()
    {
        lock (pendingRecordsLock)
        {
            var first = pendingRecords.First;
            if (first == null)
            {
                return null;
            }

            if (!exitRequested && !CanWriteBufferedRecord(first.Value))
            {
                return null;
            }

            pendingRecords.RemoveFirst();
            return first.Value.Record;
        }
    }

    private bool CanWriteBufferedRecord(BufferedVariableLogRecord bufferedRecord)
    {
        if (reorderBufferDelay <= TimeSpan.Zero)
        {
            return true;
        }

        var timestampDistance = highestBufferedTimestampUtcTicks - bufferedRecord.Record.TimestampUtcTicks;
        if (timestampDistance >= reorderBufferDelay.Ticks)
        {
            return true;
        }

        return false;
    }

    private async Task WriteRecordAsync(VariableLogRecord record)
    {
        if (logStream == null)
        {
            return;
        }

        try
        {
            await MessagePackSerializer.SerializeAsync(logStream, record);
            if (flushOnWrite)
            {
                await logStream.FlushAsync();
            }
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Error, $"Data log record could not be written: {e.Message}");
        }
    }

    private static VariableLogRecord CreateRecord(IProtocolVariable protocolVariable)
    {
        var variable = protocolVariable.Variable;
        var value = variable.GetValue();
        return new VariableLogRecord
        {
            TimestampUtcTicks = variable.Timestamp.ToUniversalTime().Ticks,
            VariableId = variable.Id,
            VariableNamespace = variable.Namespace,
            VariableName = variable.Name,
            VariableLabel = variable.Label,
            ValueType = value?.GetType().FullName ?? string.Empty,
            Value = ConvertValue(value)
        };
    }

    private static string ConvertValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
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

    private static string CreateTimestampedLogFilePath(string configuredFilePath, string? dataLogFileName, DateTime timestamp)
    {
        var hasFileExtension = !string.IsNullOrWhiteSpace(Path.GetExtension(configuredFilePath));
        var directory = hasFileExtension ? Path.GetDirectoryName(configuredFilePath) : configuredFilePath;
        var fileName = !string.IsNullOrWhiteSpace(dataLogFileName)
            ? dataLogFileName
            : hasFileExtension
                ? Path.GetFileNameWithoutExtension(configuredFilePath)
                : null;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "values";
        }

        var timestampText = timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture);
        var timestampedFileName = $"{fileName}_{timestampText}{DataLogExtension}";
        return string.IsNullOrWhiteSpace(directory)
            ? timestampedFileName
            : Path.Combine(directory, timestampedFileName);
    }

    private sealed record BufferedVariableLogRecord(VariableLogRecord Record);
}
