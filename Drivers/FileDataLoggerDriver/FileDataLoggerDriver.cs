using System.Globalization;
using System.Reflection;
using MessagePack;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.FileDataLoggerDriver;

public class FileDataLoggerDriver : DriverBase, IProtocolVariableSinkDriver, IDataLogFileNameDriver
{
    private const string DataLogExtension = ".qilog";
    private const string TimestampFormat = "yyyyMMdd'_'HH'h'mm";
    private readonly SemaphoreSlim fileLock = new(1, 1);
    private string logFilePath = Path.Combine(AppContext.BaseDirectory, "DataLogs");
    private bool append = true;
    private bool flushOnWrite;
    private FileStream? logStream;

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
    }

    public bool CanSubscribe(IProtocolVariable sourceVariable)
    {
        return Protocols
            .OfType<IProtocolVariableSinkProtocol>()
            .Any(protocol => protocol.CanProcess(sourceVariable));
    }

    public async Task OnProtocolVariableValueChangedAsync(IProtocolVariable sourceVariable)
    {
        if (State != CommunicationState.Running || logStream == null)
        {
            return;
        }

        var processedVariable = await ProcessVariableAsync(sourceVariable);
        if (processedVariable == null)
        {
            return;
        }

        var record = CreateRecord(processedVariable);
        await fileLock.WaitAsync();
        try
        {
            await MessagePackSerializer.SerializeAsync(logStream, record);
            if (flushOnWrite)
            {
                await logStream.FlushAsync();
            }
        }
        finally
        {
            fileLock.Release();
        }
    }

    public override Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            SetState(CommunicationState.Disabled);
            return Task.CompletedTask;
        }

        SetState(CommunicationState.Starting);
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
        SetState(CommunicationState.Running);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
        SetState(CommunicationState.Stopping);
        if (logStream != null)
        {
            await fileLock.WaitAsync(ct);
            try
            {
                await logStream.FlushAsync(ct);
                await logStream.DisposeAsync();
                logStream = null;
            }
            finally
            {
                fileLock.Release();
            }
        }

        SetState(CommunicationState.Stopped);
    }

    public override void Dispose()
    {
        logStream?.Dispose();
        fileLock.Dispose();
    }

    public override void Send<T>(T data)
    {
        throw new NotSupportedException("File data logger driver does not send data.");
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        throw new NotSupportedException("File data logger driver does not send data.");
    }

    private async ValueTask<IProtocolVariable?> ProcessVariableAsync(IProtocolVariable sourceVariable)
    {
        foreach (var protocol in Protocols.OfType<IProtocolVariableSinkProtocol>())
        {
            if (!protocol.CanProcess(sourceVariable))
            {
                continue;
            }

            return await protocol.ProcessObservedValueAsync(sourceVariable);
        }

        return null;
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
}
