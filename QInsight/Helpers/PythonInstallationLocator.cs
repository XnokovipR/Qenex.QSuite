using System.IO;
using Microsoft.Win32;

namespace Qenex.QInsight.Helpers;

/// <summary>One CPython installation found on this machine: its version, the DLL the scripting
/// runtime needs and the registry hive it was found in ("HKCU" = per-user, "HKLM" = all users).</summary>
public sealed record PythonInstallation(Version Version, string DllPath, string Source);

/// <summary>
/// Finds the 64-bit CPython installations the embedded scripting runtime supports
/// (pythonnet 3.1: CPython 3.10–3.14) through the PEP 514 registry keys the python.org installer
/// writes: Software\Python\PythonCore\&lt;tag&gt;\InstallPath under HKCU (per-user install) and HKLM
/// (all-users install). The DLL is python3&lt;minor&gt;.dll inside InstallPath. Installations that
/// are 32-bit, ARM64, outside the supported range or without the DLL on disk are ignored.
/// </summary>
public static class PythonInstallationLocator
{
    public static readonly Version MinSupported = new(3, 10);
    public static readonly Version MaxSupported = new(3, 14);

    public static string SupportedRangeText => $"{MinSupported}–{MaxSupported}";

    /// <summary>Supported installations, newest version first; empty when none is found.</summary>
    public static IReadOnlyList<PythonInstallation> FindSupported()
    {
        var found = new Dictionary<string, PythonInstallation>(StringComparer.OrdinalIgnoreCase);
        foreach (var (hive, source) in new[] { (RegistryHive.CurrentUser, "HKCU"), (RegistryHive.LocalMachine, "HKLM") })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var core = baseKey.OpenSubKey(@"Software\Python\PythonCore");
                if (core == null)
                {
                    continue;
                }

                foreach (var tag in core.GetSubKeyNames())
                {
                    var installation = ReadInstallation(core, tag, source);
                    if (installation != null && !found.ContainsKey(installation.DllPath))
                    {
                        found[installation.DllPath] = installation;
                    }
                }
            }
            catch (Exception)
            {
                // A hive that cannot be read counts as "nothing installed there".
            }
        }

        return found.Values.OrderByDescending(i => i.Version).ToList();
    }

    private static PythonInstallation? ReadInstallation(RegistryKey core, string tag, string source)
    {
        // PEP 514 tags of the python.org installer: "3.13" = 64-bit x64, "3.13-32" = 32-bit,
        // "3.13-arm64" = ARM64. Only the plain x64 tag matches the 64-bit QInsight process.
        if (tag.Contains('-') || !Version.TryParse(tag, out var version) || version.Major != 3)
        {
            return null;
        }

        if (version < MinSupported || version > MaxSupported)
        {
            return null;
        }

        using var key = core.OpenSubKey(tag);
        if (key == null)
        {
            return null;
        }

        if (key.GetValue("SysArchitecture") is string architecture &&
            !architecture.Equals("64bit", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        using var installPathKey = key.OpenSubKey("InstallPath");
        if (installPathKey?.GetValue(null) is not string installPath || string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }

        var dllPath = Path.Combine(installPath, $"python3{version.Minor}.dll");
        return File.Exists(dllPath) ? new PythonInstallation(version, dllPath, source) : null;
    }
}
