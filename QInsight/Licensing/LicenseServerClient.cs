using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace Qenex.QInsight.Licensing;

/// <summary>Error codes reported by the license server (plus client-side transport failures).</summary>
public enum LicenseServerError
{
    None,
    UnknownLicense,
    LicenseRevoked,
    LicenseExpired,
    LicenseNotYetValid,
    NoSeatAvailable,
    NotActivated,
    NetworkError,
    ServerError
}

/// <summary>Successful activate/heartbeat response body (camelCase JSON on the wire).</summary>
public sealed record LicenseTokenResponse(
    string Token,
    DateTime ExpiresAtUtc,
    DateTime SubscriptionEndsUtc,
    string Product,
    string Tier,
    string[] Entitlements);

public sealed record LicenseServerResult(bool Success, LicenseServerError Error, LicenseTokenResponse? Response)
{
    public static LicenseServerResult Ok(LicenseTokenResponse response) => new(true, LicenseServerError.None, response);
    public static LicenseServerResult Fail(LicenseServerError error) => new(false, error, null);
}

/// <summary>Thin HTTP wrapper for the license server endpoints. Never throws — transport
/// failures are reported as <see cref="LicenseServerError.NetworkError"/>.</summary>
public class LicenseServerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    /// <summary>The caller owns the HttpClient (BaseAddress + timeout configured there).</summary>
    public LicenseServerClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Task<LicenseServerResult> ActivateAsync(
        string licenseKey, string machineFingerprint, string? machineName, CancellationToken cancellationToken)
        => PostAsync("/api/license/activate", new { licenseKey, machineFingerprint, machineName }, cancellationToken);

    public Task<LicenseServerResult> HeartbeatAsync(
        string licenseKey, string machineFingerprint, CancellationToken cancellationToken)
        => PostAsync("/api/license/heartbeat", new { licenseKey, machineFingerprint }, cancellationToken);

    private async Task<LicenseServerResult> PostAsync(string path, object body, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(path, body, JsonOptions, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadFromJsonAsync<LicenseTokenResponse>(JsonOptions, cancellationToken);
                return string.IsNullOrWhiteSpace(payload?.Token)
                    ? LicenseServerResult.Fail(LicenseServerError.ServerError)
                    : LicenseServerResult.Ok(payload);
            }

            return LicenseServerResult.Fail(MapError(await ReadErrorCodeAsync(response, cancellationToken)));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return LicenseServerResult.Fail(LicenseServerError.NetworkError);
        }
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return document.RootElement.TryGetProperty("error", out var error) ? error.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static LicenseServerError MapError(string? code) => code switch
    {
        "unknownLicense" => LicenseServerError.UnknownLicense,
        "licenseRevoked" => LicenseServerError.LicenseRevoked,
        "licenseExpired" => LicenseServerError.LicenseExpired,
        "licenseNotYetValid" => LicenseServerError.LicenseNotYetValid,
        "noSeatAvailable" => LicenseServerError.NoSeatAvailable,
        "notActivated" => LicenseServerError.NotActivated,
        _ => LicenseServerError.ServerError
    };
}
