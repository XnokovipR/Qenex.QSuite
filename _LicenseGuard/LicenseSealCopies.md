# LicenseSeal — MASTER REGISTR validátorů (anti-tamper)

Autoritativní evidence všech nezávislých licenčních validátorů: **kde přesně**,
**jak** a **co přesně dělají**, per verze. (Zrcadlí se i v mé paměti:
`memory/license-seal-registry.md`.)

- **Předloha:** [`LicenseSealTemplate.cs`](./LicenseSealTemplate.cs) — kanonická
  metoda `SealValid()`.
- **Složka `_LicenseGuard` je JEN v gitu, NIKDE nereferencována / nekompilována.**
  Zdroj pro ruční copy-paste.
- **Nahrazuje** dřívější `LicenseGuardCopies.md` (přístup „mazání `license.json`",
  ZRUŠEN 2026-08-26 — proti patchi hlavní brány neúčinný).

---

## Princip (společný všem validátorům)

- Vloženo **přímo do akce**; volá se `if (!XSealValid()) return;`.
- **NEČTE `licenseService`** (to je místo, které útočník patchuje) — sám ověří
  token z `license.json` (ECDSA podpis + `Product` + `MachineFingerprint`).
- **fail-open:** `true` = licencováno / nejasné (chyba sondy) → povolit;
  `false` = JISTĚ nelicencováno (chybí soubor/token, špatný podpis, jiný Product,
  jiný stroj) → tiše `return` = akce se neprovede. Malformace / nedekódovatelné /
  krypto výjimka → `true`. **Expirace se záměrně neřeší** (řeší hlavní brána;
  snižuje false-positive).
- **Reakce = doména hostitele** (u pluginů: každý rozbije JEN vlastní funkčnost).
- Různost kopií dořeší **obfuskátor**; jediné, co se ručně mění, je **název metody**.

---

## Aktivní validátory

### QInsight jádro — soubor `QInsight/ViewModels/ShellWindowModel.Commands.cs`

Všechny 3 jsou přesná kopie `SealValid()` z předlohy, liší se jen názvem.

| # | Metoda | Chráněná akce | Hostitel (metoda) | Přesné místo vložení |
|---|--------|---------------|-------------------|----------------------|
| **V1a** | `TemplateSealValid()` | Založení projektu | `NewProjectAsync` | jako **1. příkaz metody**, PŘED `try` |
| **V1b** | `ArchiveSealValid()` | Otevření projektu | `OpenProjectFileAsync` | jako **1. příkaz metody**, PŘED `try` |
| **V2** | `RuntimeSealValid()` | Connect / start runtime | `ConnectAsync` | **za** blokem hlavní brány (`if (!licenseService.IsRuntimeAllowed)`), **před** Free-cap blokem (`connectSignalLimit`) |

**Volání (identické u všech):**
```csharp
if (!TemplateSealValid())   // resp. ArchiveSealValid / RuntimeSealValid
    return;
```

**Co dělá `SealValid()` (krok za krokem):**
1. Sestaví cestu `%LOCALAPPDATA%\Qenex\QInsight\license.json` (bez `AppConfig`).
2. Soubor chybí → `false` (nelicencováno). `LocalAppData` nedostupné → `true` (fail-open).
3. Přečte pole `Token` (case-insensitive). Prázdný → `false`. Nečitelný/koruptní → `true`.
4. Token musí být `QLIC1.<payload>.<sig>`; jinak → `true` (fail-open).
5. Base64url dekóduje payload+podpis; ověří **ECDSA P-256/SHA-256** vestavěným
   veřejným klíčem. Nesedí → `false`. Výjimka v kryptu → `true`.
6. Z payloadu ověří `Product == "QInsight"` a `MachineFingerprint == tento stroj`
   (SHA256(MachineGuid) lower). Nesedí → `false`.
7. Vše OK → `true`. Jakákoli neošetřená výjimka kdekoli → `true` (fail-open).

**Reakce:** `false` → tichý `return` z handleru → projekt se nezaloží/neotevře,
runtime se nespustí. Žádná hláška navíc → splývá s běžným nelicencovaným stavem.

**Nasazeno:** v1, 2026-08-26. Build 0 errors. NECOMMITNUTO.

---

## Plánované validátory (plugin fáze — zatím NEnasazeno)

Každý rozbije **vlastní** funkčnost, nezávisle, fail-open, bez čtení `licenseService`.
POZOR měřák: **nesmí zkreslit naměřená data** (spíš „odmítni/nezačni" než „vracej jiná čísla").

| Plugin | Assembly | Stav | Poznámka k reakci |
|--------|----------|------|-------------------|
| XCP | `XcpCore` | ☐ | **první na řadě**; jádro protokolu |
| Modbus | `ModbusCore` | ☐ | |
| PeakCAN | `PeakCanDriver` | ☐ | proprietární PCAN API |
| FileDataLogger | `FileDataLoggerDriver` | ☐ | formát datalogu |
| Graph | `GraphControl` | ☐ | vykreslování |
| WatchTable | `WatchTableControl` | ☐ | |
| Signal | `SignalControl` | ☐ | |

(Čisté transporty `TcpClientDriver`/`TcpServerDriver` VYPADLY — nulové know-how.)

---

## Historie verzí

- **v1 — 2026-08-26** — pivot z „mazání tokenu" na funkční validátory. Předloha
  `SealValid` + nasazení `TemplateSealValid` (New), `ArchiveSealValid` (Open),
  `RuntimeSealValid` (Connect) do `ShellWindowModel.Commands.cs`. Fail-open.
  Dočasná log-sonda přidána, ověřena a **odstraněna**. QInsight build 0 errors.
