using System.Security.Cryptography;
using System.Text.Json;

namespace Qenex.QInsight.Licensing;

/// <summary>
/// Compact signed license token: "QLIC1.&lt;base64url payload&gt;.&lt;base64url signature&gt;".
/// ECDSA P-256 over the UTF-8 JSON payload; the private key never leaves the
/// license server, the product verifies with the embedded public key.
///
/// Verification-only copy baked directly into QInsight — the signing side
/// (private key, key generation) lives on the server and is intentionally absent
/// here. Kept out of a shared assembly so there is no single licensing DLL to swap.
/// </summary>
public static class LicenseToken
{
    public const string Prefix = "QLIC1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static LicenseValidationResult Validate(
        string token,
        string publicKeyPem,
        string? expectedFingerprint = null,
        DateTimeOffset? now = null,
        TimeSpan? clockSkew = null)
    {
        var utcNow = now ?? DateTimeOffset.UtcNow;
        var skew = clockSkew ?? TimeSpan.FromMinutes(5);

        var parts = token.Split('.');
        if (parts.Length != 3 || parts[0] != Prefix)
            return LicenseValidationResult.Fail(LicenseValidationError.MalformedToken);

        byte[] payload;
        byte[] signature;
        try
        {
            payload = FromBase64Url(parts[1]);
            signature = FromBase64Url(parts[2]);
        }
        catch (FormatException)
        {
            return LicenseValidationResult.Fail(LicenseValidationError.MalformedToken);
        }

        using var key = ECDsa.Create();
        key.ImportFromPem(publicKeyPem);
        if (!key.VerifyData(payload, signature, HashAlgorithmName.SHA256))
            return LicenseValidationResult.Fail(LicenseValidationError.InvalidSignature);

        LicenseClaims? claims;
        try
        {
            claims = JsonSerializer.Deserialize<LicenseClaims>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return LicenseValidationResult.Fail(LicenseValidationError.MalformedToken);
        }

        if (claims is null)
            return LicenseValidationResult.Fail(LicenseValidationError.MalformedToken);

        if (claims.IssuedAtUtc > utcNow + skew)
            return LicenseValidationResult.Fail(LicenseValidationError.IssuedInFuture, claims);

        if (claims.ExpiresAtUtc < utcNow - skew)
            return LicenseValidationResult.Fail(LicenseValidationError.Expired, claims);

        if (expectedFingerprint is not null
            && !string.Equals(claims.MachineFingerprint, expectedFingerprint, StringComparison.Ordinal))
        {
            return LicenseValidationResult.Fail(LicenseValidationError.FingerprintMismatch, claims);
        }

        return LicenseValidationResult.Success(claims);
    }

    private static byte[] FromBase64Url(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }
}

public enum LicenseValidationError
{
    None = 0,
    MalformedToken,
    InvalidSignature,
    Expired,
    IssuedInFuture,
    FingerprintMismatch,
}

public sealed record LicenseValidationResult
{
    public bool IsValid { get; private init; }

    public LicenseValidationError Error { get; private init; }

    /// <summary>Parsed claims; set even for some failures (e.g. expiry) so the
    /// product can show a meaningful message.</summary>
    public LicenseClaims? Claims { get; private init; }

    public static LicenseValidationResult Success(LicenseClaims claims) =>
        new() { IsValid = true, Error = LicenseValidationError.None, Claims = claims };

    public static LicenseValidationResult Fail(LicenseValidationError error, LicenseClaims? claims = null) =>
        new() { IsValid = false, Error = error, Claims = claims };
}
