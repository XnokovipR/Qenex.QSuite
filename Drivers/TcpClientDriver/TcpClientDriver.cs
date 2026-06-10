using System.Net.Sockets;
using System.Reflection;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.TcpClientDriver;

public class TcpClientDriver : DriverBase
{
    private string host = "127.0.0.1";
    private int port = 5000;
    private int connectionTimeoutMs = 5000;
    private int reconnectTimeMs = 1000;
    private int numberOfReconnections = 3;
    private volatile bool exitRequested;
    private CancellationTokenSource? runCts;
    private Task? runTask;
    private TcpClient? tcpClient;

    public TcpClientDriver()
    {
        Specification = new SpecificationBase
        {
            Name = "TcpClientDriver",
            Label = "TCP Client Driver",
            Description = "TCP client driver for receiving newline-delimited data from a TCP master.",
            CreatedOn = new DateTime(2026, 6, 1),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public override void SetConfiguration()
    {
        var settings = ParseSettings(RawSettings);
        host = GetString(settings, "ip", GetString(settings, "host", host));
        port = GetInt(settings, "port", port);
        connectionTimeoutMs = Math.Max(1, GetInt(settings, "connectionTimeout", GetInt(settings, "connectionTimeoutMs", connectionTimeoutMs)));
        reconnectTimeMs = Math.Max(1, GetInt(settings, "reconnectTime", GetInt(settings, "reconnectTimeMs", reconnectTimeMs)));
        numberOfReconnections = Math.Max(0, GetInt(settings, "numberOfReconnections", GetInt(settings, "reconnections", numberOfReconnections)));
    }

    public override Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            SetState(CommunicationState.Stopped);
            return Task.CompletedTask;
        }
        if (State == CommunicationState.Running) return Task.CompletedTask;

        SetState(CommunicationState.Starting);
        exitRequested = false;
        runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        runTask = RunLoopAsync(runCts.Token);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
        SetState(CommunicationState.Stopping);
        exitRequested = true;
        if (runCts != null)
        {
            await runCts.CancelAsync();
        }

        CloseClient();
        if (runTask != null)
        {
            try
            {
                await runTask.WaitAsync(TimeSpan.FromMilliseconds(connectionTimeoutMs), ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (TimeoutException)
            {
                Logger?.Log(LogLevel.Warn, "TCP client driver did not stop before timeout.");
            }
        }

        runCts?.Dispose();
        runCts = null;
        runTask = null;
    }

    public override void Dispose()
    {
        CloseClient();
        runCts?.Dispose();
    }

    public override void Send<T>(T data)
    {
        throw new NotSupportedException("TCP client driver does not send data.");
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        throw new NotSupportedException("TCP client driver does not send data.");
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        foreach (var protocol in Protocols)
        {
            await protocol.StartAsync(ct);
        }

        SetState(CommunicationState.Running);
        var reconnectAttempt = 0;

        try
        {
            while (!ct.IsCancellationRequested && !exitRequested)
            {
                try
                {
                    using var client = await ConnectAsync(ct);
                    tcpClient = client;
                    await ReadLinesAsync(client, () => reconnectAttempt = 0, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested || exitRequested)
                {
                    break;
                }
                catch (Exception e) when (e is SocketException or IOException or TimeoutException)
                {
                    reconnectAttempt++;
                    Logger?.Log(LogLevel.Warn, $"TCP client connection to {host}:{port} failed or timed out ({reconnectAttempt}/{numberOfReconnections}): {e.Message}");
                    if (reconnectAttempt >= numberOfReconnections)
                    {
                        Logger?.Log(LogLevel.Error, $"TCP client driver reached maximum reconnection count ({numberOfReconnections}) and stopped.");
                        break;
                    }

                    await Task.Delay(reconnectTimeMs, ct);
                }
                finally
                {
                    CloseClient();
                }
            }
        }
        finally
        {
            foreach (var protocol in Protocols)
            {
                await protocol.StopAsync(CancellationToken.None);
            }

            SetState(CommunicationState.Stopped);
            exitRequested = false;
        }
    }

    private async Task<TcpClient> ConnectAsync(CancellationToken ct)
    {
        var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, ct).AsTask().WaitAsync(TimeSpan.FromMilliseconds(connectionTimeoutMs), ct);
            Logger?.Log(LogLevel.Info, $"TCP client connected to {host}:{port}.");
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private async Task ReadLinesAsync(TcpClient client, Action onLineReceived, CancellationToken ct)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream);

        while (!ct.IsCancellationRequested && !exitRequested)
        {
            var line = await reader.ReadLineAsync(ct).AsTask().WaitAsync(TimeSpan.FromMilliseconds(connectionTimeoutMs), ct);
            if (line == null)
            {
                throw new IOException("TCP master closed the connection.");
            }

            await ProcessReceivedDataAsync(line, ct);
            onLineReceived();
        }
    }

    private async Task ProcessReceivedDataAsync(string line, CancellationToken ct)
    {
        foreach (var protocol in Protocols)
        {
            if (protocol is ProtocolBase<string> stringProtocol)
            {
                await stringProtocol.AddReceivedDataToQueueAsync([line], ct);
            }
        }
    }

    private void CloseClient()
    {
        tcpClient?.Close();
        tcpClient?.Dispose();
        tcpClient = null;
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

    private static string GetString(IReadOnlyDictionary<string, string> settings, string key, string defaultValue)
    {
        return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : defaultValue;
    }

    private static int GetInt(IReadOnlyDictionary<string, string> settings, string key, int defaultValue)
    {
        return settings.TryGetValue(key, out var value) && int.TryParse(value, out var parsedValue)
            ? parsedValue
            : defaultValue;
    }
}
