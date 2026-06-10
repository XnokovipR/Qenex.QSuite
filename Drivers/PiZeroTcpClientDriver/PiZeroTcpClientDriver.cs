using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.PiZeroTcpClientDriver;

public class PiZeroTcpClientDriver : DriverBase, IProtocolVariableCommandDriver
{
    private string host = "127.0.0.1";
    private int port = 5000;
    private int connectTimeoutMs = 3000;
    private int reconnectDelayMs = 1000;
    private CancellationTokenSource? driverCancellation;
    private Task? runTask;
    private TcpClient? tcpClient;
    private StreamWriter? writer;
    private readonly SemaphoreSlim writerLock = new(1, 1);

    public PiZeroTcpClientDriver()
    {
        Specification = new SpecificationBase
        {
            Name = "PiZeroTcpClientDriver",
            Label = "Pi Zero TCP Client Driver",
            Description = "TCP client driver for Raspberry Pi Zero JSON line signal data.",
            CreatedOn = new DateTime(2026, 5, 31),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    public override void SetConfiguration()
    {
        var settings = ParseSettings(RawSettings);
        if (settings.TryGetValue("host", out var configuredHost) && !string.IsNullOrWhiteSpace(configuredHost))
        {
            host = configuredHost;
        }

        if (settings.TryGetValue("port", out var portText) && int.TryParse(portText, out var configuredPort))
        {
            port = configuredPort;
        }

        if (settings.TryGetValue("connectTimeoutMs", out var connectTimeoutText)
            && int.TryParse(connectTimeoutText, out var configuredConnectTimeout))
        {
            connectTimeoutMs = configuredConnectTimeout;
        }

        if (settings.TryGetValue("reconnectDelayMs", out var reconnectDelayText)
            && int.TryParse(reconnectDelayText, out var configuredReconnectDelay))
        {
            reconnectDelayMs = configuredReconnectDelay;
        }
    }

    public override Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            SetState(CommunicationState.Disabled);
            return Task.CompletedTask;
        }

        if (runTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        SetState(CommunicationState.Starting);
        driverCancellation?.Dispose();
        driverCancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        runTask = RunLoopAsync(driverCancellation.Token);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
        SetState(CommunicationState.Stopping);
        driverCancellation?.Cancel();
        CloseConnection();

        if (runTask != null)
        {
            try
            {
                await runTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        foreach (var protocol in Protocols)
        {
            await protocol.StopAsync(ct);
        }

        runTask = null;
        driverCancellation?.Dispose();
        driverCancellation = null;
        SetState(CommunicationState.Stopped);
    }

    public override void Dispose()
    {
        driverCancellation?.Cancel();
        CloseConnection();
        driverCancellation?.Dispose();
        writerLock.Dispose();
    }

    public override void Send<T>(T data)
    {
        SendAsync(data).GetAwaiter().GetResult();
    }

    public override async Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        if (data is not string line)
        {
            throw new NotSupportedException("Pi Zero TCP client driver only sends string JSON lines.");
        }

        await writerLock.WaitAsync(ct);
        try
        {
            if (writer == null)
            {
                Logger?.Log(LogLevel.Warn, "Pi Zero TCP client is not connected; command was not sent.");
                return;
            }

            await writer.WriteLineAsync(line.AsMemory(), ct);
            await writer.FlushAsync(ct);
        }
        finally
        {
            writerLock.Release();
        }
    }

    public bool CanSendCommand(IProtocolVariable protocolVariable)
    {
        return Protocols
            .OfType<IProtocolVariableCommandProtocol>()
            .Any(protocol => protocol.CanEncodeCommand(protocolVariable));
    }

    public async Task OnProtocolVariableCommandAsync(IProtocolVariable protocolVariable, CancellationToken ct = default)
    {
        foreach (var protocol in Protocols.OfType<IProtocolVariableCommandProtocol>())
        {
            if (!protocol.CanEncodeCommand(protocolVariable))
            {
                continue;
            }

            var command = protocol.EncodeCommand(protocolVariable);
            if (!string.IsNullOrWhiteSpace(command))
            {
                await SendAsync(command, ct);
            }

            return;
        }
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(ct);
                foreach (var protocol in Protocols)
                {
                    await protocol.StartAsync(ct);
                }

                SetState(CommunicationState.Running);
                Logger?.Log(LogLevel.Info, $"Pi Zero TCP client connected to {host}:{port}.");
                await ReadLoopAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                Logger?.Log(LogLevel.Warn, $"Pi Zero TCP client connection failed: {e.Message}");
            }
            finally
            {
                SetState(CommunicationState.Starting);
                CloseConnection();
                foreach (var protocol in Protocols)
                {
                    await protocol.StopAsync(CancellationToken.None);
                }
            }

            if (!ct.IsCancellationRequested)
            {
                await Task.Delay(reconnectDelayMs, ct);
            }
        }
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        var client = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(connectTimeoutMs);
        try
        {
            await client.ConnectAsync(host, port, timeout.Token);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        tcpClient = client;
        var stream = tcpClient.GetStream();
        writer = new StreamWriter(stream, new UTF8Encoding(false), bufferSize: 1024, leaveOpen: true)
        {
            AutoFlush = true
        };
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        if (tcpClient == null)
        {
            return;
        }

        using var reader = new StreamReader(tcpClient.GetStream(), Encoding.UTF8, leaveOpen: true);
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line == null)
            {
                break;
            }

            foreach (var protocol in Protocols.OfType<ProtocolBase<string>>())
            {
                await protocol.AddReceivedDataToQueueAsync([line], ct);
            }
        }
    }

    private void CloseConnection()
    {
        writer?.Dispose();
        writer = null;
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
}
