using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Qenex.Licensing;
using Qenex.QInsight.Licensing;
using Qenex.QSuite.Common.CoreComm;
using Qenex.QSuite.Drivers.Driver;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Protocols.Protocol;
using Qenex.QSuite.Specifications.Specification;
using Qenex.QSuite.Variables.QVariables;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QInsight.Tests.LicensingTest;

/// <summary>Console-style unit tests for the QInsight licensing client (repo convention,
/// same as the other _Tests projects). Exit code 0 = all passed.</summary>
internal static class Program
{
    private static int failures;
    private static readonly Logger Logger = new(LogLevel.Error);

    private static int Main()
    {
        MachineFingerprint_IsStableLowercaseHex();
        StoredToken_Valid_AllowsRuntime();
        StoredToken_Expired_BlocksRuntimeButKeepsClaims();
        StoredToken_WrongFingerprint_IsInvalid();
        StoredToken_WrongProduct_IsInvalid();
        StoredToken_WrongSignature_IsInvalid();
        Activate_Success_StoresTokenAndAllowsRuntime();
        Activate_UnknownLicense_ReportsError();
        Heartbeat_NetworkError_KeepsCurrentToken();
        Heartbeat_Revoked_ClearsLicense();
        Heartbeat_NotActivated_DisablesLicenseWithoutReactivating();
        StoredKeyWithoutToken_IsNotLicensedButKeepsKey();
        OfflineActivation_ValidCode_ActivatesWithoutServer();
        OfflineActivation_ForeignMachineCode_IsRejected();
        OfflineActivation_PersistsAcrossRestartAndSkipsHeartbeat();
        LicenseStore_CorruptFile_ReturnsNull();
        FreeTier_HasCommunicatedSignalLimit();
        NonFreeTierOrNoLicense_HasNoCommunicatedSignalLimit();
        CommunicatedSignals_CountsOnlyCommunicatedVariablesAcrossDriversAndProtocols();

        Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : $"{failures} TEST(S) FAILED");
        return failures == 0 ? 0 : 1;
    }

    #region Tests

    private static void MachineFingerprint_IsStableLowercaseHex()
    {
        var first = MachineFingerprint.Get();
        var second = MachineFingerprint.Get();

        Check(first == second, "fingerprint is stable across calls");
        Check(first.Length == 64, "fingerprint is 64 chars");
        Check(first.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f'), "fingerprint is lowercase hex");
    }

    private static void StoredToken_Valid_AllowsRuntime()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.StoreToken(keys.PrivateKeyPem, MakeClaims());

        harness.Service.LoadStoredLicense();

        Check(harness.Service.Status == LicenseStatus.Valid, "valid stored token -> Valid");
        Check(harness.Service.IsRuntimeAllowed, "valid stored token -> runtime allowed");
        Check(!harness.Service.IsFreeTier, "Professional tier is not Free");
    }

    private static void StoredToken_Expired_BlocksRuntimeButKeepsClaims()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.StoreToken(keys.PrivateKeyPem, MakeClaims() with
        {
            IssuedAtUtc = DateTimeOffset.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        });

        harness.Service.LoadStoredLicense();

        Check(harness.Service.Status == LicenseStatus.Expired, "expired stored token -> Expired");
        Check(!harness.Service.IsRuntimeAllowed, "expired stored token -> runtime blocked");
        Check(harness.Service.CurrentClaims is not null, "expired stored token -> claims still available for UI");
    }

    private static void StoredToken_WrongFingerprint_IsInvalid()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.StoreToken(keys.PrivateKeyPem, MakeClaims() with { MachineFingerprint = "someone-elses-machine" });

        harness.Service.LoadStoredLicense();

        Check(harness.Service.Status == LicenseStatus.Invalid, "foreign fingerprint -> Invalid");
        Check(!harness.Service.IsRuntimeAllowed, "foreign fingerprint -> runtime blocked");
    }

    private static void StoredToken_WrongProduct_IsInvalid()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.StoreToken(keys.PrivateKeyPem, MakeClaims() with { Product = "OtherProduct" });

        harness.Service.LoadStoredLicense();

        Check(harness.Service.Status == LicenseStatus.Invalid, "foreign product -> Invalid");
    }

    private static void StoredToken_WrongSignature_IsInvalid()
    {
        var signingKeys = LicenseToken.CreateKeyPair();
        var otherKeys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(otherKeys.PublicKeyPem);
        harness.StoreToken(signingKeys.PrivateKeyPem, MakeClaims());

        harness.Service.LoadStoredLicense();

        Check(harness.Service.Status == LicenseStatus.Invalid, "wrong signing key -> Invalid");
        Check(!harness.Service.IsRuntimeAllowed, "wrong signing key -> runtime blocked");
    }

    private static void Activate_Success_StoresTokenAndAllowsRuntime()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.Handler.NextResponse = TokenResponse(keys.PrivateKeyPem, MakeClaims() with { Tier = "Free" });

        var result = harness.Service.ActivateAsync("QNX-ABCD-EFGH-JKLM-NPQR").GetAwaiter().GetResult();

        Check(result.Success, "activation succeeds");
        Check(harness.Service.Status == LicenseStatus.Valid, "activation -> Valid");
        Check(harness.Service.IsRuntimeAllowed, "activation -> runtime allowed");
        Check(harness.Service.IsFreeTier, "Free tier detected");
        Check(harness.Store.TryLoad()?.LicenseKey == "QNX-ABCD-EFGH-JKLM-NPQR", "license persisted to store");
    }

    private static void Activate_UnknownLicense_ReportsError()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.Handler.NextResponse = ErrorResponse(HttpStatusCode.NotFound, "unknownLicense");

        var result = harness.Service.ActivateAsync("QNX-ABCD-EFGH-JKLM-NPQR").GetAwaiter().GetResult();

        Check(!result.Success, "unknown key -> activation fails");
        Check(result.Error == LicenseServerError.UnknownLicense, "unknown key -> UnknownLicense error");
        Check(harness.Service.Status == LicenseStatus.NotLicensed, "unknown key -> still not licensed");
        Check(harness.Store.TryLoad() is null, "unknown key -> nothing persisted");
    }

    private static void Heartbeat_NetworkError_KeepsCurrentToken()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.StoreToken(keys.PrivateKeyPem, MakeClaims());
        harness.Service.LoadStoredLicense();
        harness.Handler.ThrowNetworkError = true;

        harness.Service.RefreshAsync().GetAwaiter().GetResult();

        Check(harness.Service.Status == LicenseStatus.Valid, "network error -> offline grace keeps license valid");
        Check(harness.Service.IsRuntimeAllowed, "network error -> runtime still allowed");
    }

    private static void Heartbeat_Revoked_ClearsLicense()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.StoreToken(keys.PrivateKeyPem, MakeClaims());
        harness.Service.LoadStoredLicense();
        harness.Handler.NextResponse = ErrorResponse(HttpStatusCode.Forbidden, "licenseRevoked");

        harness.Service.RefreshAsync().GetAwaiter().GetResult();

        Check(harness.Service.Status == LicenseStatus.Invalid, "revoked -> Invalid immediately (no grace)");
        Check(!harness.Service.IsRuntimeAllowed, "revoked -> runtime blocked");
        Check(harness.Store.TryLoad() is null, "revoked -> stored license deleted");
    }

    private static void Heartbeat_NotActivated_DisablesLicenseWithoutReactivating()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.StoreToken(keys.PrivateKeyPem, MakeClaims());
        harness.Service.LoadStoredLicense();
        harness.Handler.NextResponse = ErrorResponse(HttpStatusCode.NotFound, "notActivated");

        harness.Service.RefreshAsync().GetAwaiter().GetResult();

        Check(harness.Service.Status == LicenseStatus.NotLicensed, "deactivated seat -> NotLicensed");
        Check(!harness.Service.IsRuntimeAllowed, "deactivated seat -> runtime blocked");
        Check(harness.Handler.RequestCount == 1, "deactivated seat -> NO silent re-activation request");
        var stored = harness.Store.TryLoad();
        Check(stored?.LicenseKey == "QNX-ABCD-EFGH-JKLM-NPQR", "deactivated seat -> key kept for re-activation");
        Check(stored?.Token == string.Empty, "deactivated seat -> token dropped");
    }

    private static void StoredKeyWithoutToken_IsNotLicensedButKeepsKey()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.Store.Save(new StoredLicense("QNX-ABCD-EFGH-JKLM-NPQR", string.Empty));

        harness.Service.LoadStoredLicense();

        Check(harness.Service.Status == LicenseStatus.NotLicensed, "key-only store -> NotLicensed after restart");
        Check(harness.Service.LicenseKey == "QNX-ABCD-EFGH-JKLM-NPQR", "key-only store -> key available for prefill");
    }

    private static void OfflineActivation_ValidCode_ActivatesWithoutServer()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        var code = LicenseToken.Sign(MakeClaims(), keys.PrivateKeyPem);

        var status = harness.Service.ActivateOffline($"  {code}  ");

        Check(status == LicenseStatus.Valid, "offline code -> Valid (whitespace tolerated)");
        Check(harness.Service.IsRuntimeAllowed, "offline code -> runtime allowed");
        Check(harness.Handler.RequestCount == 0, "offline activation -> no server request");
        Check(harness.Store.TryLoad() is { LicenseKey: "" }, "offline activation -> stored without a key");
    }

    private static void OfflineActivation_ForeignMachineCode_IsRejected()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        var code = LicenseToken.Sign(MakeClaims() with { MachineFingerprint = "someone-elses-machine" },
            keys.PrivateKeyPem);

        var status = harness.Service.ActivateOffline(code);

        Check(status == LicenseStatus.Invalid, "foreign offline code -> Invalid");
        Check(!harness.Service.IsRuntimeAllowed, "foreign offline code -> runtime blocked");
        Check(harness.Store.TryLoad() is null, "foreign offline code -> nothing persisted");
    }

    private static void OfflineActivation_PersistsAcrossRestartAndSkipsHeartbeat()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.Service.ActivateOffline(LicenseToken.Sign(MakeClaims(), keys.PrivateKeyPem));

        // Simulated restart: reload from the same store.
        harness.Service.LoadStoredLicense();
        Check(harness.Service.Status == LicenseStatus.Valid, "offline license survives a restart");
        Check(harness.Service.LicenseKey is null, "offline license -> no key after reload");

        // Without a key there is nothing to heartbeat — the subscription-long token
        // must never be replaced by a short-lived online one.
        harness.Service.RefreshAsync().GetAwaiter().GetResult();
        Check(harness.Handler.RequestCount == 0, "offline license -> heartbeat is skipped");
        Check(harness.Service.IsRuntimeAllowed, "offline license -> runtime still allowed");
    }

    private static void FreeTier_HasCommunicatedSignalLimit()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var harness = new Harness(keys.PublicKeyPem);
        harness.StoreToken(keys.PrivateKeyPem, MakeClaims() with { Tier = "Free" });
        harness.Service.LoadStoredLicense();

        var limit = CommunicatedSignals.GetLimit(harness.Service);

        Check(limit == LicensingConstants.FreeMaxCommunicatedSignals, "Free tier -> communicated signal limit applies");
        Check(LicensingConstants.FreeMaxCommunicatedSignals == 5, "Free tier limit is 5");
        Check(6 > limit, "6 communicated signals exceed the Free limit");
        Check(!(5 > limit), "5 communicated signals fit the Free limit");
        Check(CommunicatedSignals.BuildOverLimitMessage(6, 5)
              == "Free license: the project has 6 communicated signals, the limit is 5.",
            "over-limit message names count and limit");
    }

    private static void NonFreeTierOrNoLicense_HasNoCommunicatedSignalLimit()
    {
        var keys = LicenseToken.CreateKeyPair();
        using var licensedHarness = new Harness(keys.PublicKeyPem);
        licensedHarness.StoreToken(keys.PrivateKeyPem, MakeClaims());
        licensedHarness.Service.LoadStoredLicense();

        Check(CommunicatedSignals.GetLimit(licensedHarness.Service) is null, "Professional tier -> no signal limit");

        using var unlicensedHarness = new Harness(keys.PublicKeyPem);
        unlicensedHarness.Service.LoadStoredLicense();

        Check(CommunicatedSignals.GetLimit(unlicensedHarness.Service) is null,
            "no license -> no signal limit (runtime is blocked by the license guard itself)");
    }

    private static void CommunicatedSignals_CountsOnlyCommunicatedVariablesAcrossDriversAndProtocols()
    {
        var protocolA = new FakeProtocol();
        protocolA.Variables.Add(new ProtocolVariable { IsCommunicated = true });
        protocolA.Variables.Add(new ProtocolVariable { IsCommunicated = false });
        var protocolB = new FakeProtocol();
        protocolB.Variables.Add(new ProtocolVariable { IsCommunicated = true });
        var driverWithTwoProtocols = new FakeDriver(protocolA, protocolB);

        var protocolC = new FakeProtocol();
        protocolC.Variables.Add(new ProtocolVariable { IsCommunicated = true });
        protocolC.Variables.Add(new ProtocolVariable { IsCommunicated = false });
        var secondDriver = new FakeDriver(protocolC);

        Check(CommunicatedSignals.Count(new IDriverBase[] { driverWithTwoProtocols, secondDriver }) == 3,
            "count spans all drivers and protocols and counts only IsCommunicated");
        Check(CommunicatedSignals.Count(new IDriverBase[] { new FakeDriver() }) == 0, "driver without protocols -> 0");
        Check(CommunicatedSignals.Count((IEnumerable<IDriverBase>?)null) == 0, "no drivers -> 0");

        // Data-logger and replay drivers mirror project variables as their own protocol
        // variables (isCommunicated=true) — they must not consume the Free-tier limit.
        var loggerProtocol = new FakeProtocol();
        loggerProtocol.Variables.Add(new ProtocolVariable { IsCommunicated = true });
        loggerProtocol.Variables.Add(new ProtocolVariable { IsCommunicated = true });
        var replayProtocol = new FakeProtocol();
        replayProtocol.Variables.Add(new ProtocolVariable { IsCommunicated = true });
        replayProtocol.Variables.Add(new ProtocolVariable { IsCommunicated = true });
        var project = new IDriverBase[]
        {
            driverWithTwoProtocols,
            new FakeSinkDriver(loggerProtocol),
            new FakeReplayDriver(replayProtocol)
        };

        Check(CommunicatedSignals.Count(project) == 2,
            "sink (logger) and replay drivers are excluded from the count");
    }

    private static void LicenseStore_CorruptFile_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"qinsight-license-test-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{ this is not json");
            var store = new LicenseStore(path, Logger);

            Check(store.TryLoad() is null, "corrupt license file -> treated as no license");
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Helpers

    private static LicenseClaims MakeClaims() => new()
    {
        LicenseId = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        Product = LicensingConstants.ProductName,
        Tier = "Professional",
        Entitlements = ["scripting"],
        MachineFingerprint = MachineFingerprint.Get(),
        IssuedAtUtc = DateTimeOffset.UtcNow,
        ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
        SubscriptionEndsUtc = DateTimeOffset.UtcNow.AddDays(365)
    };

    private static HttpResponseMessage TokenResponse(string privateKeyPem, LicenseClaims claims)
    {
        var token = LicenseToken.Sign(claims, privateKeyPem);
        var body = new
        {
            token,
            expiresAtUtc = claims.ExpiresAtUtc.UtcDateTime,
            subscriptionEndsUtc = claims.SubscriptionEndsUtc?.UtcDateTime ?? default,
            product = claims.Product,
            tier = claims.Tier,
            entitlements = claims.Entitlements
        };
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage ErrorResponse(HttpStatusCode status, string code)
        => new(status)
        {
            Content = new StringContent($"{{\"error\":\"{code}\"}}", Encoding.UTF8, "application/json")
        };

    private static void Check(bool condition, string name)
    {
        Console.WriteLine($"{(condition ? "PASS" : "FAIL")}  {name}");
        if (!condition)
        {
            failures++;
        }
    }

    /// <summary>Bundles a LicenseService with a temp-file store and a fake HTTP handler.</summary>
    private sealed class Harness : IDisposable
    {
        private readonly string storePath;
        private readonly HttpClient httpClient;

        public Harness(string publicKeyPem)
        {
            storePath = Path.Combine(Path.GetTempPath(), $"qinsight-license-test-{Guid.NewGuid():N}.json");
            Store = new LicenseStore(storePath, Logger);
            Handler = new FakeHttpHandler();
            httpClient = new HttpClient(Handler) { BaseAddress = new Uri("https://localhost.test") };
            Service = new LicenseService(Logger, Store, new LicenseServerClient(httpClient), publicKeyPem);
        }

        public LicenseStore Store { get; }
        public FakeHttpHandler Handler { get; }
        public LicenseService Service { get; }

        public void StoreToken(string privateKeyPem, LicenseClaims claims)
            => Store.Save(new StoredLicense("QNX-ABCD-EFGH-JKLM-NPQR", LicenseToken.Sign(claims, privateKeyPem)));

        public void Dispose()
        {
            Service.Dispose();
            httpClient.Dispose();
            File.Delete(storePath);
        }
    }

    /// <summary>Minimal driver/protocol graph for CommunicatedSignals.Count; only Protocols
    /// and Variables matter, the rest of the interfaces is inert.</summary>
    private class FakeDriver(params IProtocolBase[] protocols) : IDriverBase
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string RawSettings { get; set; } = string.Empty;
        public string RawEncryptedSettings { get; set; } = string.Empty;
        public string DefaultRawSettings => string.Empty;
        public IList<IProtocolBase> Protocols { get; init; } = protocols.ToList();
        public bool IsEnabled { get; set; }
        public CommunicationState State => default;
        public string? StateMessage => null;
        public event EventHandler<CommunicationStateChangedEventArgs>? StateChanged { add { } remove { } }
        public ISpecification Specification => null!;
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public void SetConfiguration() { }
        public void AddProtocol(IProtocolBase protocol) => Protocols.Add(protocol);
        public void AddProtocols(IEnumerable<IProtocolBase> newProtocols) { }
        public void RemoveProtocol(IProtocolBase protocol) { }
        public void RemoveProtocol(string protocolName) { }
        public void Send<T>(T data) { }
        public Task SendAsync<T>(T data, CancellationToken ct = default) => Task.CompletedTask;
    }

    /// <summary>Data-logger-like driver: consumes protocol variables, does not communicate.</summary>
    private sealed class FakeSinkDriver(params IProtocolBase[] protocols)
        : FakeDriver(protocols), IProtocolVariableSinkDriver
    {
        public bool CanSubscribe(IProtocolVariable sourceVariable) => false;
        public Task OnProtocolVariableValueChangedAsync(IProtocolVariable sourceVariable) => Task.CompletedTask;
    }

    private sealed class FakeReplayDriver(params IProtocolBase[] protocols)
        : FakeDriver(protocols), IReplayDriver
    {
        public event EventHandler? ReplayCompleted { add { } remove { } }
        public event EventHandler<ReplayProgressChangedEventArgs>? ReplayProgressChanged { add { } remove { } }
        public TimeSpan CurrentTime => TimeSpan.Zero;
        public TimeSpan Duration => TimeSpan.Zero;
        public bool IsPaused => false;
        public bool IsDataLoaded => false;
        public bool IsDataLoading => false;
        public void StartLoadingData(CancellationToken ct = default) { }
        public void CancelLoadingData() { }
        public void Pause() { }
        public void Resume() { }
        public Task SeekAsync(TimeSpan position, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeProtocol : IProtocolBase
    {
        public int Id { get; set; }
        public IList<IProtocolVariable> Variables { get; set; } = [];
        public string RawSettings { get; set; } = string.Empty;
        public string RawEncryptedSettings { get; set; } = string.Empty;
        public string DefaultRawSettings => string.Empty;
        public IReadOnlyList<string>? CompatibleDrivers => null;
        public ILogger? Logger { get; set; }
        public bool IsEnabled { get; set; }
        public CommunicationState State => default;
        public string? StateMessage => null;
        public event EventHandler<CommunicationStateChangedEventArgs>? StateChanged { add { } remove { } }
        public ISpecification Specification => null!;
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public void SetConfiguration() { }
        public string CreateDefaultCommParam(IVariableBase variable, IEnumerable<IVarEvent> variableEvents) => string.Empty;
        public IProtocolVariable? CreateProtocolVariable(IVariableBase variable, string commParams, bool isCommunicated) => null;
        public IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IVarEvent variableEvent, string id) => null;
        public IProtocolVariable? CreateProtocolVariable(IVariableBase variable, IEnumerable<IVarEvent> variableEvents, string commParams, bool isCommunicated) => null;
        public void AddVariable(IProtocolVariable protocolVariable) => Variables.Add(protocolVariable);
        public void RemoveProtocolVariable(IProtocolVariable variable) { }
        public void RemoveProtocolVariable(string variableName) { }
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        public HttpResponseMessage? NextResponse { get; set; }
        public bool ThrowNetworkError { get; set; }
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            if (ThrowNetworkError)
            {
                throw new HttpRequestException("simulated network failure");
            }

            var response = NextResponse ?? new HttpResponseMessage(HttpStatusCode.InternalServerError);
            NextResponse = null;
            return Task.FromResult(response);
        }
    }

    #endregion
}
