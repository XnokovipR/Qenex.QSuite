# Ukládání controls na workspace v Calibru

## Context

Uživatel chce vysvětlit, jak Calibro (WPF .NET aplikace) reprezentuje vizuální „controls" (gauges, slidery, tabulky, grafy, parametry…) umístěné na ploše workspacu a jak se tyto controls (jejich pozice, rozměry, stav, vazba na proměnné) persistují do projektového souboru a opět načítají. Jde o čistě výkladovou odpověď — žádné změny v kódu.

---

## 1. In-memory model

### Workspace
- [`WorkspaceViewModel`](Calibro/ViewModels/WorkspaceViewModel.cs:16) — třída označená `[DataContract]`. Sama je dokument-pane v RadDockingu (Telerik) a zároveň serializovatelný kontejner.
- Klíčová kolekce: [`Controls`](Calibro/ViewModels/WorkspaceViewModel.cs:98) typu `BindableCollection<ISimpleControl>` (Caliburn.Micro). Drží všechny controls na ploše.
- Další serializované vlastnosti workspacu: `Header`, `WinTitle`, `EnabledForLoad`.

### Control
Společné rozhraní je [`ISimpleControl`](Controls/ISimpleControl/ISimpleControl.cs:16). Každý konkrétní typ controlu má vlastní projekt v `Source\Calibro\Controls\` — `GaugeControl`, `GraphControl`, `NotepadControl`, `ParamControl`, `ScalarParamControl`, `SignalSliderControl`, `SimpleButtonControl`, `SingleParamControl`, `StringParamControl`, `TableControl`, `TextBoxControl`, `TriStateSwitchControl`.

Rozhraní rozlišuje **persistované** a **runtime-only** vlastnosti pomocí `[DataMember]` / `[IgnoreDataMember]`:

**Persistované do projektu** (`[DataMember]`, [ISimpleControl.cs:61-92](Controls/ISimpleControl/ISimpleControl.cs:61)):
- `X`, `Y`, `Width`, `Height` — pozice a velikost na canvasu
- `Tag` — uživatelský popisek
- `IsLocked` — zámek proti přesunu/změně velikosti
- `IsSimulated`, `IsLogSimulated`, `IsLogged`, `IsRun` — runtime příznaky uložené i mezi spuštěními
- `LinkedVariables : List<string>` — **plná jména** (cesta `Signature\Namespace\Var`) proměnných, na které je control navázán. Toto je jediná vazba na proměnnou, která se ukládá.

**Runtime-only** (`[IgnoreDataMember]`): `Variables : ObservableCollection<Variable>` (resolvované instance), `Image`, `BackgroundColor`, `ControlName`, `ControlDescription`, akce uploadu/downloadu, EventAggregator atd. Ty se po načtení znovu zaplní.

### Zobrazení
- [`WorkspaceView.xaml`](Calibro/Views/WorkspaceView.xaml) používá [`WorkspaceViewControl`](Calibro/Views/WorkspaceViewControl.cs) (`ItemsControl`) + [`WorkspaceCanvas`](Calibro/Views/WorkspaceCanvas.cs) jako `ItemsPanel`. Itemy mají `Canvas.Left` a `Canvas.Top` nabindované přímo na `X` a `Y` controlu — tedy mutace properties na ViewModelu = pohyb na ploše.
- Nový control vznikne drag-dropem z [`ControlsView`](Calibro/Views/ControlsView.xaml); [`WorkspaceDragDropBehavior`](Calibro/Views/WorkspaceDragDropBehavior.cs) udělá `Activator.CreateInstance` typu controlu a nastaví `X`/`Y` podle místa dropu, případně nabinduje proměnnou taženou z `VariablesView`.

---

## 2. Projektový soubor (`.calibro`)

Projekt je **ZIP archiv** (`Ionic.Zip`) chráněný heslem (`AesCrypt.DecryptString(...)` v [MainViewModel.Ribbon.cs:163](Calibro/ViewModels/MainViewModel.Ribbon.cs:163)). Obsahuje:

| Soubor v ZIPu | Co je uvnitř | Jak se serializuje |
|---|---|---|
| `WorkspaceLayout.xml` | RadDocking layout (rozmístění panes) | `radDocking.SaveLayout()` |
| `{Header}.ws` (pro každý workspace) | `WorkspaceViewModel` + jeho `Controls` | `DataContractSerializer` |
| `ProjectSettings.xml` | `ProjectData` — IDs projektu, kanály, var-driver mapping, event/trigger sekce | `XmlSerializer` přes [`XmlBasic<ProjectData>`](Calibro/Helpers/XmlBasic.cs:5) |
| `*.edml` | EDML streamy s definicemi proměnných/signálů | Originální stream zkopírovaný 1:1 |

---

## 3. Save flow — [`MainViewModel.Ribbon.cs:149-246`](Calibro/ViewModels/MainViewModel.Ribbon.cs:149)

1. Backup stávajícího `.calibro` na `.stemp`, otevření `ZipFile` s heslem.
2. `radDocking.SaveLayout(...)` → `WorkspaceLayout.xml` do ZIPu.
3. Pro každý dokumentový pane (`Panes.Where(ws => ws.IsDocument)`):
   ```csharp
   var serializer = new DataContractSerializer(
       typeof(WorkspaceViewModel),
       ((WorkspaceViewModel)workspacePane).GetTypesOfControls() // známé subtypy
   );
   serializer.WriteObject(xw, (WorkspaceViewModel)workspacePane);
   ```
   Výsledek se uloží jako `{Header}.ws`. `GetTypesOfControls()` ([WorkspaceViewModel.cs:290](Calibro/ViewModels/WorkspaceViewModel.cs:290)) seskenuje aktuální obsah `Controls` a vrátí pole `Type[]` — to musí `DataContractSerializer` znát, aby uměl serializovat polymorfní položky v `BindableCollection<ISimpleControl>`.
4. `XmlBasic<ProjectData>.SaveToStream(...)` → `ProjectSettings.xml`.
5. EDML streamy zkopírovány do ZIPu beze změny.

Výstupní XML jednoho controlu vypadá zhruba takto (jen `[DataMember]` vlastnosti):
```xml
<GaugeControl X="120" Y="80" Width="200" Height="150"
              IsLocked="false" IsRun="true" Tag="...">
  <LinkedVariables>
    <string>ECU\Engine\RPM</string>
  </LinkedVariables>
</GaugeControl>
```

---

## 4. Load flow — [`MainViewModel.Helpers.cs:149-346`](Calibro/ViewModels/MainViewModel.Helpers.cs:149)

1. Otevření ZIPu, načtení všech entries do paměti.
2. `XmlBasic<ProjectData>.LoadFromStream(...)` → obnoví `projectSettings` (kanály, var-drv assignmenty, eventy/triggery).
3. Načtení EDML → `VariablesViewModel` má znovu k dispozici instance `Variable` pojmenované podle plných cest.
4. Pro každý `*.ws` ([MainViewModel.Helpers.cs:250-335](Calibro/ViewModels/MainViewModel.Helpers.cs:250)):
   ```csharp
   Type[] controlsTypes = ControlsViewModel.Controls.Select(c => c.GetType()).ToArray();
   var dcs = new DataContractSerializer(typeof(WorkspaceViewModel), controlsTypes);
   ws = (WorkspaceViewModel)dcs.ReadObject(xr);
   ws.Initialize(eventAgregator);
   ```
   Pozor: známé subtypy se zde berou z **palety dostupných controls** (`ControlsViewModel.Controls`), ne z workspace souboru — proto musí být všechny typy registrované.
5. **Rebinding proměnných** ([Helpers.cs:262-282](Calibro/ViewModels/MainViewModel.Helpers.cs:262)):
   ```csharp
   foreach (var control in ws.Controls)
       foreach (var lVar in control.LinkedVariables) {
           Variable variable = VariablesViewModel.GetVariableByName(lVar);
           if (variable == null) { /* log warning, skip */ continue; }
           control.BindAfterDeserialization(variable);   // znovu naplní runtime Variables
           // + reassignment do EventTrigger / VarDriver podle projectSettings
       }
   ```
   `BindAfterDeserialization` ([ISimpleControl.cs:104](Controls/ISimpleControl/ISimpleControl.cs:104)) je implementační hook v každém konkrétním controlu — typicky přidá `Variable` do `Variables`, napojí konvertery, default hodnotu a UI binding.
6. `[OnDeserialized]` ([WorkspaceViewModel.cs:314](Calibro/ViewModels/WorkspaceViewModel.cs:314)) zapne `IsNotifying` a doplní `EnabledForLoad` default.
7. Workspace se přidá do `Panes`, RadDocking následně aplikuje `WorkspaceLayout.xml` a pane se objeví na svém místě.

---

## 5. Stručné shrnutí

- **Co je v paměti**: `WorkspaceViewModel.Controls` = `BindableCollection<ISimpleControl>`, kde každý prvek nese pozici, velikost a `LinkedVariables` (seznam jmen).
- **Co se ukládá**: jen `[DataMember]` vlastnosti — geometrie, runtime flagy a **jména** propojených proměnných. Samotná data proměnných jsou v EDML / ProjectSettings.
- **Jak**: workspace → `DataContractSerializer` → `{Header}.ws` uvnitř šifrovaného ZIPu (`.calibro`).
- **Při otevření**: ZIP se rozbalí, ProjectSettings + EDML obnoví proměnné, každý `.ws` se deserializuje a controly se přes `BindAfterDeserialization` znovu napojí na živé `Variable` instance podle jmen v `LinkedVariables`.

## Verification

Žádné změny v kódu nejsou navrhovány — toto je pouze popis stávajícího chování. Ověření v rámci přečtených souborů:
- [Calibro/ViewModels/WorkspaceViewModel.cs:16-324](Calibro/ViewModels/WorkspaceViewModel.cs:16)
- [Controls/ISimpleControl/ISimpleControl.cs:16-123](Controls/ISimpleControl/ISimpleControl.cs:16)
- [Calibro/ViewModels/MainViewModel.Ribbon.cs:149-246](Calibro/ViewModels/MainViewModel.Ribbon.cs:149) (save)
- [Calibro/ViewModels/MainViewModel.Helpers.cs:149-346](Calibro/ViewModels/MainViewModel.Helpers.cs:149) (load)
- [Calibro/Helpers/XmlBasic.cs](Calibro/Helpers/XmlBasic.cs) (XML helper pro ProjectData)
