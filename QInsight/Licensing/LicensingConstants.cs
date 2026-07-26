namespace Qenex.QInsight.Licensing;

/// <summary>Licensing configuration baked into the QInsight binary.</summary>
public static class LicensingConstants
{
    public const string ServerBaseUrl = "https://srv.qenex.net";

    /// <summary>Product name expected in license token claims; tokens issued for other products are rejected.</summary>
    public const string ProductName = "QInsight";

    /// <summary>Maximum number of communicated signals (protocol variables with IsCommunicated)
    /// allowed by the Free tier, counted across the whole project. Other tiers are unlimited.</summary>
    public const int FreeMaxCommunicatedSignals = 5;

    /// <summary>Production ECDSA P-256 public key used for offline token validation.
    /// The matching private key exists only on the license server.</summary>
    public const string PublicKeyPem = """
        -----BEGIN PUBLIC KEY-----
        MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEOACOwsviai2bRt16xogoH6vXPVtV
        ziUZNXothbQLrl6y3K3GZfh49fajb50QT1zJ9XGjhJOc6SRNACtipO8eiA==
        -----END PUBLIC KEY-----
        """;
}
