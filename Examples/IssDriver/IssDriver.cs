using System.Reflection;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Examples.IssDriver;

/// <summary>
/// Example driver: reads live data from a public REST API — the current position of the
/// International Space Station. Every period it performs one HTTP GET and hands the raw JSON
/// response to its protocols; turning the JSON into variable values is the protocol's job
/// (see the IssJsonProtocol example).
/// This is about the smallest possible real-data driver: read-only, one fixed URL, one setting.
/// </summary>
public class IssDriver : DriverBase, ITransportSource<string>
{
    // Free, key-less API returning one flat JSON object with the current ISS position.
    // Be polite to the public service: do not poll faster than ~1 request per second.
    private const string Url = "https://api.wheretheiss.at/v1/satellites/25544";

    private int periodMs = 1000;

    private HttpClient? httpClient;
    private CancellationTokenSource? runCts;
    private Task? runTask;

    public IssDriver()
    {
        Specification = new SpecificationBase
        {
            Name = "IssDriver",
            Label = "ISS Position (example)",
            Description = "Example driver polling the ISS position from a public REST API.",
            CreatedOn = new DateTime(2026, 7, 27),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    #region Configuration

    // Shown pre-filled when the driver is added in Project Configuration.
    public override string DefaultRawSettings => "periodMs=1000";

    public override void SetConfiguration()
    {
        // "key=value;..." — a missing or invalid key silently keeps the default.
        foreach (var part in RawSettings.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pair.Length == 2
                && pair[0].Equals("periodMs", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(pair[1], out var parsedPeriod))
            {
                // The API asks for at most ~1 request per second, so slower is allowed, faster is not.
                periodMs = Math.Max(parsedPeriod, 1000);
            }
        }
    }

    #endregion

    #region Driver control

    public override async Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            SetState(CommunicationState.Disabled);
            return;
        }

        if (State == CommunicationState.Running || runTask is { IsCompleted: false })
        {
            return;
        }

        SetState(CommunicationState.Starting);

        // Short timeout so a slow or unreachable server cannot block the loop for long.
        httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        foreach (var protocol in Protocols)
        {
            await protocol.StartAsync(ct);
        }

        runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        runTask = RunLoopAsync(runCts.Token);
        SetState(CommunicationState.Running);
    }

    public override async Task StopAsync(CancellationToken ct = default)
    {
        SetState(CommunicationState.Stopping);

        if (runCts != null)
        {
            await runCts.CancelAsync();
        }

        if (runTask != null)
        {
            try
            {
                await runTask.WaitAsync(TimeSpan.FromSeconds(5), ct);
            }
            catch (OperationCanceledException)
            {
            }
            catch (TimeoutException)
            {
                Logger?.Log(LogLevel.Warn, "ISS position: the polling loop did not stop before timeout.");
            }
        }

        foreach (var protocol in Protocols)
        {
            await protocol.StopAsync(ct);
        }

        httpClient?.Dispose();
        httpClient = null;
        runCts?.Dispose();
        runCts = null;
        runTask = null;
        SetState(CommunicationState.Stopped);
    }

    public override void Dispose()
    {
        httpClient?.Dispose();
        runCts?.Dispose();
    }

    #endregion

    #region Communication

    // The API is read-only, so there is nothing to send. A writable driver would push the
    // encoded data to the device here (see the TempSensorDriver example).
    public override void Send<T>(T data)
    {
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Polling loop

    private async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // One GET returns one JSON object carrying several signals at once
                    // (latitude, longitude, altitude, velocity, ...).
                    var json = await httpClient!.GetStringAsync(Url, ct);

                    foreach (var protocol in Protocols)
                    {
                        if (protocol is ProtocolBase<string> textProtocol)
                        {
                            await textProtocol.AddReceivedDataToQueueAsync([json], ct);
                        }
                    }
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
                {
                    // Network hiccups are normal with a public API: log it and try again next period.
                    Logger?.Log(LogLevel.Warn, $"ISS position: request failed ({e.Message}).");
                }

                await Task.Delay(periodMs, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Normal stop.
        }
        catch (Exception e)
        {
            Logger?.Log(LogLevel.Error, $"ISS position loop failed: {e.Message}");
            SetState(CommunicationState.Faulted, e.Message);
        }
    }

    #endregion
}
