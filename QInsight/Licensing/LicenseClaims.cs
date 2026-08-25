namespace Qenex.QInsight.Licensing;

/// <summary>
/// Payload of a signed license token. Issued by the license server, verified
/// offline by the product using the embedded public key. Baked directly into
/// QInsight (no shared licensing package) so the verification path has no single
/// external assembly that could be swapped out to bypass it.
/// </summary>
public sealed record LicenseClaims
{
    public required Guid LicenseId { get; init; }

    public required Guid OrganizationId { get; init; }

    public required string Product { get; init; }

    public required string Tier { get; init; }

    public IReadOnlyList<string> Entitlements { get; init; } = [];

    /// <summary>Hardware fingerprint of the machine the token is bound to.</summary>
    public required string MachineFingerprint { get; init; }

    public required DateTimeOffset IssuedAtUtc { get; init; }

    /// <summary>Hard token expiry — includes the offline grace period.</summary>
    public required DateTimeOffset ExpiresAtUtc { get; init; }

    /// <summary>End of the underlying subscription (informational for the UI).</summary>
    public DateTimeOffset? SubscriptionEndsUtc { get; init; }
}
