using System.IO;
using System.Text.Json;
using Qenex.QSuite.LogSystems.LogSystem;

namespace Qenex.QInsight.Licensing;

/// <summary>License data persisted on this machine. An empty token with a kept key means
/// the machine was deactivated in the portal — the key stays for easy re-activation.
/// A token without a key is an offline (air-gapped) activation — no heartbeats run and
/// the token simply lasts until its own expiry.</summary>
public sealed record StoredLicense(string LicenseKey, string Token);

/// <summary>Persists the license key and the last issued token. Lives in %LOCALAPPDATA% (with
/// the other per-user files, see <see cref="AppConfig.AppDataPaths"/>) because the application
/// directory is not writable when installed under Program Files. A missing or corrupt file simply
/// means "no license" — storage errors must never crash the application.</summary>
public class LicenseStore
{
    private readonly string filePath;
    private readonly Logger? logger;

    public LicenseStore(Logger? logger = null)
        : this(AppConfig.AppDataPaths.GetFilePath("license.json"), logger)
    {
    }

    /// <summary>Test seam: redirects storage to an arbitrary file.</summary>
    public LicenseStore(string filePath, Logger? logger = null)
    {
        this.filePath = filePath;
        this.logger = logger;
    }

    public StoredLicense? TryLoad()
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var stored = JsonSerializer.Deserialize<StoredLicense>(File.ReadAllText(filePath));
            if (stored is null
                || (string.IsNullOrWhiteSpace(stored.LicenseKey) && string.IsNullOrWhiteSpace(stored.Token)))
            {
                return null;
            }

            return stored with
            {
                LicenseKey = stored.LicenseKey ?? string.Empty,
                Token = stored.Token ?? string.Empty
            };
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Warn, $"Failed to load license file \"{filePath}\": {e.Message}");
            return null;
        }
    }

    public void Save(StoredLicense license)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, JsonSerializer.Serialize(license));
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Warn, $"Failed to save license file \"{filePath}\": {e.Message}");
        }
    }

    public void Delete()
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception e)
        {
            logger?.Log(LogLevel.Warn, $"Failed to delete license file \"{filePath}\": {e.Message}");
        }
    }
}
