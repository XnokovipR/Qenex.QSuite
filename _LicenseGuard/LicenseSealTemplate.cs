// =============================================================================
// PŘEDLOHA nezávislého licenčního validátoru  —  NEREFERENCOVAT (zdroj pro kopie)
//
// Tento soubor je JEN v gitu ve složce _LicenseGuard. NENÍ v žádném .csproj a
// NESMÍ se nikam referencovat — jinak vznikne sdílený typ = jedno vyměnitelné
// místo. Slouží výhradně jako zdroj pro RUČNÍ rozkopírování (viz LicenseSealCopies.md).
//
// ÚČEL (po pivotu 2026-08-26): „defense in depth" vedle hlavní brány
// (`LicenseService`). Každá kopie je samostatná metoda vložená přímo do akce
// (New/Open/Connect) a NEČTE `licenseService` (to je místo, které útočník patchuje).
// Sama ověří token z license.json. Handler ji volá jako:  if (!XSealValid()) return;
//
// SÉMANTIKA (fail-open, ROZHODNUTO Radek 2026-08-26):
//   true  = licencováno NEBO nejasné (chyba sondy) → povolit (nikdy nezablokovat
//           platícího zákazníka)
//   false = JISTĚ nelicencováno (chybí soubor/token, špatný podpis, jiný Product,
//           jiný stroj) → tiše se zachovat jako nelicencovaný (refuse akce)
//   Malformace / nedekódovatelné / krypto výjimka → true (fail-open).
//   Expirace se ZÁMĚRNĚ neřeší (řeší hlavní brána; snižuje false-positive).
//
// ROZKOPÍROVÁNÍ: metodu zkopíruj do třídy s handlerem, přejmenuj (typ nemá, jen
// název metody + je to jediná věc, co se liší). Lokální funkce Dec/Field a lokální
// konstanty se nekryjí (lokální scope) → kopie mohou stát vedle sebe. Marker
// komentář ať jde s tím. Různost dořeší obfuskátor.
//
// Verze: v1 (2026-08-26). Nasazeno v ShellWindowModel.Commands.cs jako
//   TemplateSealValid (New), ArchiveSealValid (Open), RuntimeSealValid (Connect).
// =============================================================================

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

// LICENSE-SEAL COPY v1 — sync z _LicenseGuard
private static bool SealValid()
{
    try
    {
        const string pem =
            "-----BEGIN PUBLIC KEY-----\n" +
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEOACOwsviai2bRt16xogoH6vXPVtV\n" +
            "ziUZNXothbQLrl6y3K3GZfh49fajb50QT1zJ9XGjhJOc6SRNACtipO8eiA==\n" +
            "-----END PUBLIC KEY-----";
        const string product = "QInsight";
        const string prefix = "QLIC1";

        static byte[] Dec(string v)
        {
            var s = v.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
        }

        static string? Field(JsonElement o, string n)
        {
            if (o.ValueKind != JsonValueKind.Object) return null;
            foreach (var p in o.EnumerateObject())
                if (string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase))
                    return p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : null;
            return null;
        }

        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(baseDir)) return true;
        var path = Path.Combine(baseDir, "Qenex", "QInsight", "license.json");
        if (!File.Exists(path)) return false;

        string? token;
        using (var sd = JsonDocument.Parse(File.ReadAllText(path)))
            token = Field(sd.RootElement, "Token");
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parts = token.Split('.');
        if (parts.Length != 3 || parts[0] != prefix) return true;

        var payload = Dec(parts[1]);
        var signature = Dec(parts[2]);

        using (var key = ECDsa.Create())
        {
            key.ImportFromPem(pem);
            if (!key.VerifyData(payload, signature, HashAlgorithmName.SHA256)) return false;
        }

        using (var pd = JsonDocument.Parse(payload))
        {
            var pr = Field(pd.RootElement, "Product");
            if (pr is not null && !string.Equals(pr, product, StringComparison.Ordinal)) return false;

            var fp = Field(pd.RootElement, "MachineFingerprint");
            if (fp is not null)
            {
                string src;
                try
                {
                    using var rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                    var g = rk?.GetValue("MachineGuid") as string;
                    src = string.IsNullOrWhiteSpace(g) ? Environment.MachineName : g;
                }
                catch { src = Environment.MachineName; }

                var mine = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(src)));
                if (!string.Equals(fp, mine, StringComparison.Ordinal)) return false;
            }
        }

        return true;
    }
    catch
    {
        return true; // fail-open: never block a legitimate user on a probe error
    }
}
