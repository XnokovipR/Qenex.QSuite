using System.Text.RegularExpressions;

namespace Qenex.QInsight.Licensing;

/// <summary>Human-readable license key (QNX-XXXX-XXXX-XXXX-XXXX). The key only
/// identifies a license record on the server; it carries no entitlements.
///
/// Format-check-only copy baked into QInsight; key generation lives on the server.</summary>
public static partial class LicenseKey
{
    [GeneratedRegex("^QNX(-[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{4}){4}$")]
    private static partial Regex FormatRegex();

    /// <summary>Checks the key shape only; a well-formed key may still be unknown to the server.</summary>
    public static bool IsValidFormat(string? key)
        => key is not null && FormatRegex().IsMatch(key);
}
