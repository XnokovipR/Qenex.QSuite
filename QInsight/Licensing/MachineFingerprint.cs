using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Qenex.QInsight.Licensing;

/// <summary>Stable machine identity sent to the license server and compared against token claims.
/// Based on the Windows MachineGuid, which is created at OS install time and survives hardware
/// changes. The value must stay byte-identical across activate/heartbeat/validate (ordinal
/// comparison), hence always lowercase hex.</summary>
public static class MachineFingerprint
{
    private static string? cached;

    /// <summary>True when MachineGuid was unavailable and the machine name was hashed instead.</summary>
    public static bool UsedMachineNameFallback { get; private set; }

    public static string Get() => cached ??= Compute();

    private static string Compute()
    {
        var machineGuid = ReadMachineGuid();
        UsedMachineNameFallback = machineGuid is null;
        var source = machineGuid ?? Environment.MachineName;
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private static string? ReadMachineGuid()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var value = key?.GetValue("MachineGuid") as string;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }
}
