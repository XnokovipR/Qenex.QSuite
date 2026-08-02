namespace Qenex.QInsight.Helpers;

/// <summary>Formats plugin versions per the QENEX convention: three components
/// (major.minor.revision). The fourth assembly component is never shown; in
/// <see cref="Version"/> terms the displayed third component is <see cref="Version.Build"/>.</summary>
public static class VersionDisplay
{
    public static string ToDisplayString(this Version? version)
        => version is null ? string.Empty : version.ToString(version.Build >= 0 ? 3 : 2);
}
