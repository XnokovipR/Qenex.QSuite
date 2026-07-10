using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using Qenex.Licensing;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QInsight.Licensing;

public enum LicenseStatus
{
    NotLicensed,
    Valid,
    Expired,
    Invalid
}

/// <summary>Owns the licensing state of the running application: loads the stored license,
/// validates the signed token offline (embedded public key + machine fingerprint), renews it
/// via background heartbeats and exposes whether runtime features are allowed.
/// Offline grace equals the token validity issued by the server; a confirmed server refusal
/// (revoked/expired/unknown) ends the license immediately.</summary>
public class LicenseService : IDisposable
{
    private static readonly TimeSpan RevalidationInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromHours(24);

    private readonly Logger logger;
    private readonly LicenseStore store;
    private readonly LicenseServerClient serverClient;
    private readonly string publicKeyPem;
    private readonly HttpClient? ownedHttpClient;
    private readonly CancellationTokenSource disposeCts = new();
    private DispatcherTimer? revalidationTimer;
    private DateTimeOffset lastSuccessfulHeartbeatUtc = DateTimeOffset.MinValue;
    private bool disposed;

    public LicenseService(Logger logger)
        : this(logger, new LicenseStore(logger), null, null)
    {
    }

    /// <summary>Test seam: inject a redirected store, a client with a fake HttpMessageHandler
    /// and a test signing key. Production always uses the embedded key.</summary>
    public LicenseService(Logger logger, LicenseStore store, LicenseServerClient? serverClient, string? publicKeyPem)
    {
        this.logger = logger;
        this.store = store;
        this.publicKeyPem = publicKeyPem ?? LicensingConstants.PublicKeyPem;
        if (serverClient is null)
        {
            ownedHttpClient = new HttpClient
            {
                BaseAddress = new Uri(LicensingConstants.ServerBaseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            };
            this.serverClient = new LicenseServerClient(ownedHttpClient);
        }
        else
        {
            this.serverClient = serverClient;
        }
    }

    public LicenseStatus Status { get; private set; } = LicenseStatus.NotLicensed;

    /// <summary>Claims of the last known token; kept even when Expired/Invalid so the UI can
    /// describe the license that stopped working.</summary>
    public LicenseClaims? CurrentClaims { get; private set; }

    public string? LicenseKey { get; private set; }

    public bool IsFreeTier => string.Equals(CurrentClaims?.Tier, "Free", StringComparison.OrdinalIgnoreCase);

    /// <summary>Evaluated at call time so a token crossing its hard expiry blocks the runtime
    /// even between revalidation ticks.</summary>
    public bool IsRuntimeAllowed =>
        Status == LicenseStatus.Valid
        && CurrentClaims is { } claims
        && claims.ExpiresAtUtc >= DateTimeOffset.UtcNow;

    /// <summary>Raised on the UI thread whenever the license state changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Loads the stored license and starts background renewal. Never blocks startup:
    /// offline validation is local and the first heartbeat runs fire-and-forget.</summary>
    public void Initialize()
    {
        _ = MachineFingerprint.Get();
        if (MachineFingerprint.UsedMachineNameFallback)
        {
            logger.Log(LogLevel.Warn, "MachineGuid not available; machine fingerprint falls back to the machine name.");
        }

        LoadStoredLicense();

        revalidationTimer = new DispatcherTimer { Interval = RevalidationInterval };
        revalidationTimer.Tick += OnRevalidationTick;
        revalidationTimer.Start();

        // The startup license state is logged only AFTER the first server check completes —
        // the offline-valid token may still be refused by the server (license removed,
        // seat deactivated), and logging "licensed" first would contradict the follow-up.
        _ = RunHeartbeatSafeAsync(logStartupSummary: true);
    }

    public async Task<LicenseServerResult> ActivateAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        var result = await serverClient.ActivateAsync(
            licenseKey, MachineFingerprint.Get(), Environment.MachineName, cancellationToken);
        if (!result.Success)
        {
            return result;
        }

        if (!TryAcceptToken(licenseKey, result.Response!.Token))
        {
            // The envelope said success but the signed token does not validate for this
            // machine/product — trust only the token.
            logger.Log(LogLevel.Warn, "License activation returned a token that failed offline validation.");
            return LicenseServerResult.Fail(LicenseServerError.ServerError);
        }

        logger.Log(LogLevel.Info, $"License activated ({CurrentClaims!.Tier}, valid until {CurrentClaims.ExpiresAtUtc:u}).");
        return result;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        revalidationTimer?.Stop();
        disposeCts.Cancel();
        disposeCts.Dispose();
        ownedHttpClient?.Dispose();
    }

    private void LogStartupLicenseState()
    {
        if (Status != LicenseStatus.Valid)
        {
            logger.Log(LogLevel.Warn, "QInsight is NOT licensed — runtime and replay are disabled. Open Help -> License to activate.");
        }
        else if (IsFreeTier)
        {
            logger.Log(LogLevel.Info, "FREE NON-COMMERCIAL LICENCE — QInsight is licensed for personal/non-commercial use only.");
        }
        else
        {
            logger.Log(LogLevel.Info, $"QInsight is licensed ({CurrentClaims!.Tier}), subscription ends {CurrentClaims.SubscriptionEndsUtc?.ToLocalTime():d}.");
        }
    }

    /// <summary>Loads and offline-validates the license persisted on this machine.
    /// Part of Initialize(); public for deterministic tests without the timer.</summary>
    public void LoadStoredLicense()
    {
        var stored = store.TryLoad();
        if (stored is null)
        {
            Status = LicenseStatus.NotLicensed;
            return;
        }

        LicenseKey = stored.LicenseKey;
        if (string.IsNullOrWhiteSpace(stored.Token))
        {
            // Key-only record: the machine was deactivated; the key is kept for re-activation.
            Status = LicenseStatus.NotLicensed;
            return;
        }

        Status = EvaluateToken(stored.Token, out var claims);
        CurrentClaims = claims;
        if (Status != LicenseStatus.Valid)
        {
            logger.Log(LogLevel.Warn, $"Stored license token is not valid ({Status}).");
        }
    }

    /// <summary>Offline validation of a signed token: signature, expiry, machine fingerprint
    /// and product. Claims are returned even for expired tokens so the UI can display them.</summary>
    private LicenseStatus EvaluateToken(string token, out LicenseClaims? claims)
    {
        var result = LicenseToken.Validate(token, publicKeyPem, MachineFingerprint.Get());
        claims = result.Claims;
        if (result.IsValid)
        {
            return string.Equals(result.Claims!.Product, LicensingConstants.ProductName, StringComparison.OrdinalIgnoreCase)
                ? LicenseStatus.Valid
                : LicenseStatus.Invalid;
        }

        return result.Error == LicenseValidationError.Expired ? LicenseStatus.Expired : LicenseStatus.Invalid;
    }

    private bool TryAcceptToken(string licenseKey, string token)
    {
        if (EvaluateToken(token, out var claims) != LicenseStatus.Valid)
        {
            return false;
        }

        store.Save(new StoredLicense(licenseKey, token));
        LicenseKey = licenseKey;
        CurrentClaims = claims;
        Status = LicenseStatus.Valid;
        lastSuccessfulHeartbeatUtc = DateTimeOffset.UtcNow;
        RaiseStateChanged();
        return true;
    }

    private void OnRevalidationTick(object? sender, EventArgs e)
    {
        // A valid token may cross its hard expiry mid-session; flip the state so bound
        // commands refresh (IsRuntimeAllowed already answers false at call time).
        if (Status == LicenseStatus.Valid && CurrentClaims is { } claims && claims.ExpiresAtUtc < DateTimeOffset.UtcNow)
        {
            Status = LicenseStatus.Expired;
            logger.Log(LogLevel.Warn, "License token expired; runtime features are disabled until the next successful renewal.");
            RaiseStateChanged();
        }

        if (DateTimeOffset.UtcNow - lastSuccessfulHeartbeatUtc >= HeartbeatInterval)
        {
            _ = RunHeartbeatSafeAsync();
        }
    }

    private async Task RunHeartbeatSafeAsync(bool logStartupSummary = false)
    {
        try
        {
            await RefreshAsync(disposeCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Shutting down — no summary.
            return;
        }
        catch (Exception e)
        {
            logger.Log(LogLevel.Warn, $"License heartbeat failed unexpectedly: {e.Message}");
        }

        if (logStartupSummary)
        {
            LogStartupLicenseState();
        }
    }

    /// <summary>Renews the token via a server heartbeat. Called by the background timer;
    /// safe to call on demand.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var licenseKey = LicenseKey;
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            return;
        }

        var result = await serverClient.HeartbeatAsync(licenseKey, MachineFingerprint.Get(), cancellationToken);
        if (result.Success)
        {
            if (!TryAcceptToken(licenseKey, result.Response!.Token))
            {
                logger.Log(LogLevel.Warn, "License heartbeat returned a token that failed offline validation; keeping the current one.");
            }

            return;
        }

        switch (result.Error)
        {
            case LicenseServerError.NetworkError:
            case LicenseServerError.ServerError:
                // Offline grace: the current token stays valid until its own expiry.
                logger.Log(LogLevel.Info, $"License heartbeat unavailable ({result.Error}); running on the current token.");
                break;

            case LicenseServerError.NotActivated:
                // The seat for this machine was deactivated (typically on purpose in the
                // account portal). Never re-activate silently — that would override the
                // user's decision and could steal a seat back from another machine. The
                // key is kept so re-activation in Help -> License is a single click.
                HandleDeactivated(licenseKey);
                break;

            case LicenseServerError.LicenseRevoked:
            case LicenseServerError.LicenseExpired:
            case LicenseServerError.UnknownLicense:
                HandleServerRefusal(result.Error);
                break;

            default:
                logger.Log(LogLevel.Warn, $"License heartbeat rejected ({result.Error}); running on the current token.");
                break;
        }
    }

    private void HandleDeactivated(string licenseKey)
    {
        logger.Log(LogLevel.Warn,
            "This machine was deactivated in the account portal — runtime and replay are disabled. Open Help -> License to activate again.");
        // Keep the key (dialog prefill after restart), drop the token — it is signed and
        // would otherwise still pass offline validation until its expiry.
        store.Save(new StoredLicense(licenseKey, string.Empty));
        CurrentClaims = null;
        Status = LicenseStatus.NotLicensed;
        RaiseStateChanged();
    }

    private void HandleServerRefusal(LicenseServerError error)
    {
        // Grace covers being offline, not a confirmed refusal by the server.
        logger.Log(LogLevel.Warn, $"License server refused the license ({error}); runtime features are disabled.");
        store.Delete();
        Status = error == LicenseServerError.LicenseExpired ? LicenseStatus.Expired : LicenseStatus.Invalid;
        RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => StateChanged?.Invoke(this, EventArgs.Empty));
        }
        else
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
