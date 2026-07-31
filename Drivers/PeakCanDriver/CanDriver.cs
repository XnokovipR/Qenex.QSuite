using System.Globalization;
using System.Reflection;
using System.Text;
using Peak.Can.Basic;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;

namespace Qenex.QSuite.Drivers.PeakCanDriver;

public class CanDriver : DriverBase, IProtocolVariableCommandDriver, ITransportSource<CanFrame>
{
    #region Fields

    private const uint DefaultBitrate = 250_000;

    // One driver instance == one physical PCAN device, identified by its device id
    // (PCAN_DEVICE_ID, set in hardware), running at one bitrate. No channels.
    private uint deviceId;
    private uint bitrate = DefaultBitrate;
    private TPCANBaudrate baudrate = TPCANBaudrate.PCAN_BAUD_250K;

    private ushort channelHandle = PCANBasic.PCAN_NONEBUS;
    private bool isInitialized;

    private AutoResetEvent? receiveEvent;
    private volatile bool exitRequested;
    private CancellationTokenSource? runCts;
    private Task? runTask;

    #endregion
    
    #region Constructors

    public CanDriver()
    {
        Specification = new SpecificationBase()
        {
            Name = "PeakCANDriver",
            Label = "PEAK CAN Adapter",
            Description = "CAN bus access via a PEAK PCAN-USB adapter; carries CAN frames.",
            CreatedOn = new DateTime(2021, 11, 21),
            Version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0),
            Author = "Qenex",
            Company = "QENEX Ltd."
        };
    }

    #endregion

    // deviceId is the PCAN_DEVICE_ID set in hardware (hex, 0..FF); 0x51 is only a placeholder the
    // operator must replace with the actual device id. bitrate is in bit/s (see Bitrates map).
    public override string DefaultRawSettings => "deviceId=0x51;bitrate=250000";

    public override void SetConfiguration()
    {
        var settings = ParseSettings(RawSettings);
        deviceId = GetHexId(settings, "deviceId", deviceId);
        bitrate = GetUInt(settings, "bitrate", bitrate);

        if (Bitrates.TryGetValue(bitrate, out var mappedBaudrate))
        {
            baudrate = mappedBaudrate;
        }
        else
        {
            Logger?.Log(LogLevel.Warn, $"Unsupported CAN bitrate {bitrate}; falling back to {DefaultBitrate}.");
            bitrate = DefaultBitrate;
            baudrate = TPCANBaudrate.PCAN_BAUD_250K;
        }
    }

    #region Driver control

    public override Task StartAsync(CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            SetState(CommunicationState.Stopped);
            return Task.CompletedTask;
        }

        if (State == CommunicationState.Running)
        {
            return Task.CompletedTask;
        }

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
                Logger?.Log(LogLevel.Warn, $"PeakCAN driver '{Label}' did not stop before timeout.");
            }
        }

        runCts?.Dispose();
        runCts = null;
        runTask = null;
    }

    public override void Dispose()
    {
        Disconnect();
        receiveEvent?.Dispose();
        receiveEvent = null;
        runCts?.Dispose();
    }

    #endregion

    #region Connection

    private void Connect()
    {
        var handle = FindChannelByDeviceId();

        var status = PCANBasic.Initialize(handle, baudrate);
        if (status != TPCANStatus.PCAN_ERROR_OK)
        {
            throw new PeakCanException(status, $"Failed to initialize PCAN channel for deviceId=0x{deviceId:X2} at {bitrate} bit/s. {GetErrorText(status)}");
        }

        channelHandle = handle;
        isInitialized = true;
        Logger?.Log(LogLevel.Info, $"PeakCAN driver '{Label}' connected (deviceId=0x{deviceId:X2}, {bitrate} bit/s).");
    }

    private ushort FindChannelByDeviceId()
    {
        // PEAK-documented device identification: scan the PCAN-USB channels, read each channel's
        // persistent PCAN_DEVICE_ID (readable without initializing) and match the configured id.
        // The id is stored in the hardware, so the match survives the adapter moving between USB ports.
        var matches = new List<ushort>();
        foreach (var handle in UsbChannels)
        {
            var status = PCANBasic.GetValue(handle, TPCANParameter.PCAN_DEVICE_ID, out uint id, sizeof(uint));
            if (status == TPCANStatus.PCAN_ERROR_OK && id == deviceId)
            {
                matches.Add(handle);
            }
        }

        if (matches.Count == 0)
        {
            throw new PeakCanException(TPCANStatus.PCAN_ERROR_ILLHW,
                $"No PCAN-USB device with deviceId=0x{deviceId:X2} found.");
        }

        if (matches.Count > 1)
        {
            throw new PeakCanException(TPCANStatus.PCAN_ERROR_ILLHW,
                $"Multiple PCAN-USB devices share deviceId=0x{deviceId:X2}; assign unique Device IDs in PEAK Settings.");
        }

        return matches[0];
    }

    private void Disconnect()
    {
        // Only ever uninitialize our own handle — never PCAN_NONEBUS, which would reset every channel
        // in the process (including other driver instances).
        if (!isInitialized || channelHandle == PCANBasic.PCAN_NONEBUS)
        {
            return;
        }

        var status = PCANBasic.Uninitialize(channelHandle);
        if (status != TPCANStatus.PCAN_ERROR_OK)
        {
            Logger?.Log(LogLevel.Warn, $"PeakCAN driver '{Label}' uninitialize returned: {GetErrorText(status)}");
        }

        isInitialized = false;
        channelHandle = PCANBasic.PCAN_NONEBUS;
    }

    private static string GetErrorText(TPCANStatus status)
    {
        var buffer = new StringBuilder(256);
        return PCANBasic.GetErrorText(status, 0, buffer) == TPCANStatus.PCAN_ERROR_OK
            ? buffer.ToString()
            : status.ToString();
    }

    #endregion

    #region Receive loop

    private async Task RunLoopAsync(CancellationToken ct)
    {
        string? stopMessage = null;
        try
        {
            Connect();
            SetupReceiveEvent();

            foreach (var protocol in Protocols)
            {
                // The bus is usable from here on: give transmitting protocols (e.g. an XCP master)
                // their TX path before they start.
                if (protocol is ITransportProtocol<CanFrame> transportProtocol)
                {
                    transportProtocol.SetTransmitter((frame, token) => SendAsync(frame, token));
                }

                await protocol.StartAsync(ct);
            }

            SetState(CommunicationState.Running);

            while (!ct.IsCancellationRequested && !exitRequested)
            {
                await WaitOneAsync(receiveEvent!, ct);
                if (ct.IsCancellationRequested || exitRequested)
                {
                    break;
                }

                var frames = ReadFrames();
                if (frames.Count > 0)
                {
                    await DispatchAsync(frames, ct);
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested || exitRequested)
        {
            // Normal stop.
        }
        catch (Exception e)
        {
            stopMessage = e.Message;
            Logger?.Log(LogLevel.Error, $"PeakCAN driver '{Label}' run loop failed: {e.Message}");
        }
        finally
        {
            foreach (var protocol in Protocols)
            {
                await protocol.StopAsync(CancellationToken.None);

                if (protocol is ITransportProtocol<CanFrame> transportProtocol)
                {
                    transportProtocol.SetTransmitter(null);
                }
            }

            Disconnect();
            SetState(CommunicationState.Stopped, stopMessage);
            exitRequested = false;
        }
    }

    private void SetupReceiveEvent()
    {
        receiveEvent?.Dispose();
        receiveEvent = new AutoResetEvent(false);

        // PCAN-Basic signals this OS event whenever a frame arrives, so the driver doesn't have to poll.
        // The native API needs the raw OS handle, so DangerousGetHandle() is required here; the event is
        // kept alive for the driver's lifetime and the handle is handed over exactly once. Receiving runs
        // async via RegisterWaitForSingleObject — no dedicated thread, no Thread.Abort.
        var eventHandle = (uint)receiveEvent.SafeWaitHandle.DangerousGetHandle().ToInt64();
        var status = PCANBasic.SetValue(channelHandle, TPCANParameter.PCAN_RECEIVE_EVENT, ref eventHandle, sizeof(uint));
        if (status != TPCANStatus.PCAN_ERROR_OK)
        {
            throw new PeakCanException(status, $"Failed to register the receive event: {GetErrorText(status)}");
        }
    }

    private List<CanFrame> ReadFrames()
    {
        var frames = new List<CanFrame>();

        TPCANStatus status;
        while ((status = PCANBasic.Read(channelHandle, out var message, out var timestamp)) == TPCANStatus.PCAN_ERROR_OK)
        {
            // Forward data frames only; skip status, error and remote-request frames.
            const TPCANMessageType nonData = TPCANMessageType.PCAN_MESSAGE_STATUS
                                             | TPCANMessageType.PCAN_MESSAGE_ERRFRAME
                                             | TPCANMessageType.PCAN_MESSAGE_RTR;
            if ((message.MSGTYPE & nonData) != 0)
            {
                continue;
            }

            var isExtended = (message.MSGTYPE & TPCANMessageType.PCAN_MESSAGE_EXTENDED) != 0;
            var data = message.DATA.AsSpan(0, message.LEN).ToArray();
            frames.Add(new CanFrame(message.ID, data, isExtended, ToMicroseconds(timestamp)));
        }

        if (status != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
        {
            Logger?.Log(LogLevel.Warn, $"PeakCAN driver '{Label}' read returned: {GetErrorText(status)}");
        }

        return frames;
    }

    private async Task DispatchAsync(IReadOnlyList<CanFrame> frames, CancellationToken ct)
    {
        foreach (var protocol in Protocols)
        {
            if (protocol is ProtocolBase<CanFrame> canProtocol)
            {
                await canProtocol.AddReceivedDataToQueueAsync(frames, ct);
            }
        }
    }

    private static ulong ToMicroseconds(TPCANTimestamp timestamp)
    {
        return timestamp.micros
               + 1000UL * timestamp.millis
               + 1000UL * 0x1_0000_0000UL * timestamp.millis_overflow;
    }

    // Awaits an OS wait handle without blocking a thread — bridges the PCAN receive event into async.
    private static async Task WaitOneAsync(WaitHandle waitHandle, CancellationToken ct)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var ctr = ct.Register(static state => ((TaskCompletionSource)state!).TrySetCanceled(), tcs);
        var registration = ThreadPool.RegisterWaitForSingleObject(
            waitHandle,
            static (state, _) => ((TaskCompletionSource)state!).TrySetResult(),
            tcs,
            Timeout.InfiniteTimeSpan,
            executeOnlyOnce: true);

        try
        {
            await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            registration.Unregister(null);
        }
    }

    #endregion

    #region Communication

    public override void Send<T>(T data)
    {
        if (data is CanFrame frame)
        {
            SendFrame(frame);
            return;
        }

        throw new NotSupportedException($"PeakCAN driver can only send {nameof(CanFrame)} data.");
    }

    public override Task SendAsync<T>(T data, CancellationToken ct = default)
    {
        Send(data);
        return Task.CompletedTask;
    }

    // Operator writes: the module wires variable value-changed notifications to this driver
    // (IProtocolVariableCommandDriver); the driver delegates to the owning protocol, which executes
    // the write as a protocol transaction (e.g. XCP SET_MTA + DOWNLOAD) over this driver's TX path.
    public bool CanSendCommand(IProtocolVariable protocolVariable)
    {
        return Protocols
            .OfType<IProtocolVariableWriteProtocol>()
            .Any(protocol => protocol.CanWriteVariable(protocolVariable));
    }

    public async Task OnProtocolVariableCommandAsync(IProtocolVariable protocolVariable, CancellationToken ct = default)
    {
        foreach (var protocol in Protocols.OfType<IProtocolVariableWriteProtocol>())
        {
            if (!protocol.CanWriteVariable(protocolVariable))
            {
                continue;
            }

            await protocol.WriteVariableAsync(protocolVariable, ct);
            return;
        }
    }

    private void SendFrame(CanFrame frame)
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException("PeakCAN driver is not connected.");
        }

        var length = (byte)Math.Min(frame.Data.Length, 8);
        var message = new TPCANMsg
        {
            ID = frame.CanId,
            MSGTYPE = frame.IsExtended ? TPCANMessageType.PCAN_MESSAGE_EXTENDED : TPCANMessageType.PCAN_MESSAGE_STANDARD,
            LEN = length,
            DATA = new byte[8],
        };
        Array.Copy(frame.Data, message.DATA, length);

        var status = PCANBasic.Write(channelHandle, ref message);
        if (status != TPCANStatus.PCAN_ERROR_OK)
        {
            throw new PeakCanException(status, $"Failed to send CAN frame id=0x{frame.CanId:X}: {GetErrorText(status)}");
        }
    }

    #endregion

    #region Configuration helpers

    // Supported classic CAN bitrates mapped to the PCAN-Basic baudrate enum.
    private static readonly IReadOnlyDictionary<uint, TPCANBaudrate> Bitrates = new Dictionary<uint, TPCANBaudrate>
    {
        [50_000] = TPCANBaudrate.PCAN_BAUD_50K,
        [100_000] = TPCANBaudrate.PCAN_BAUD_100K,
        [125_000] = TPCANBaudrate.PCAN_BAUD_125K,
        [250_000] = TPCANBaudrate.PCAN_BAUD_250K,
        [500_000] = TPCANBaudrate.PCAN_BAUD_500K,
        [800_000] = TPCANBaudrate.PCAN_BAUD_800K,
        [1_000_000] = TPCANBaudrate.PCAN_BAUD_1M,
    };

    // PCAN-USB channel handles scanned when resolving a device by its id.
    private static readonly ushort[] UsbChannels =
    {
        PCANBasic.PCAN_USBBUS1, PCANBasic.PCAN_USBBUS2, PCANBasic.PCAN_USBBUS3, PCANBasic.PCAN_USBBUS4,
        PCANBasic.PCAN_USBBUS5, PCANBasic.PCAN_USBBUS6, PCANBasic.PCAN_USBBUS7, PCANBasic.PCAN_USBBUS8,
        PCANBasic.PCAN_USBBUS9, PCANBasic.PCAN_USBBUS10, PCANBasic.PCAN_USBBUS11, PCANBasic.PCAN_USBBUS12,
        PCANBasic.PCAN_USBBUS13, PCANBasic.PCAN_USBBUS14, PCANBasic.PCAN_USBBUS15, PCANBasic.PCAN_USBBUS16,
    };

    private static Dictionary<string, string> ParseSettings(string rawSettings)
    {
        return rawSettings
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1].Trim('"'), StringComparer.OrdinalIgnoreCase);
    }

    private uint GetUInt(IReadOnlyDictionary<string, string> settings, string key, uint defaultValue)
    {
        if (!settings.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        if (uint.TryParse(value, out var parsed))
        {
            return parsed;
        }

        // A typo must not pass silently — the driver would run with a value the operator never chose.
        Logger?.Log(LogLevel.Warn, $"PEAK CAN: invalid value '{value}' for setting '{key}', using {defaultValue}.");
        return defaultValue;
    }

    // Device id is entered in hexadecimal to match PEAK Settings (range 0..FF), e.g. "01", "0x1A" or "FFh".
    private uint GetHexId(IReadOnlyDictionary<string, string> settings, string key, uint defaultValue)
    {
        if (!settings.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        var text = value.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
        }
        else if (text.EndsWith("h", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^1];
        }

        if (uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        Logger?.Log(LogLevel.Warn, $"PEAK CAN: invalid hex value '{value}' for setting '{key}', using 0x{defaultValue:X}.");
        return defaultValue;
    }

    #endregion
}
