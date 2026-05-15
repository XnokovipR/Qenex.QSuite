# Qenex.QSuite.Scripting.Script

## Scope

This file applies to the `Qenex.QSuite.Scripting.Script` project.

It supplements:

- root `AGENTS.md`
- `Scripting/AGENTS.md`

## Purpose

`Qenex.QSuite.Scripting.Script` defines the shared script abstraction layer for QSuite.

It provides the minimal contracts and common metadata that concrete script implementations and, scripting engine.

Current contract surface:

- `IScriptBase`
- `ScriptExecutionMode`
- `ScriptFileType`
- `ScriptRunState`

This project should describe what a script is at a general level. It should not describe how a script is edited, hosted, executed by a specific runtime, displayed in WPF, or bound to a specific device/protocol implementation.

## Project Role

This project is responsible for:

- Common script interfaces.
- Common script execution metadata.
- Common script file/type identifiers.
- Common script run-state identifiers.
- Stable abstractions used by multiple scripting-related projects.

This project is not responsible for:

- Python-specific implementation details.
- Python runtime hosting or `pythonnet` integration.
- Script editor UI.
- QInsight docking panes, syntax highlighting UI, or script settings windows.
- Variable binding implementation.
- Logging implementation.
- Driver, protocol, device, or hardware behavior.
- Project zip loading or XML module parsing.

## Dependency Rules

This project must remain dependency-light.

Current intended dependency shape:

```text
Script -> no QSuite project references
PythonScript -> Script
ScriptingEngine -> Script
```

Rules:

- Do not add project references to `Script` without explicit approval.
- Do not reference `PythonScript`, `ScriptingEngine`, QInsight, drivers, protocols, variables, logging, controls, or module projects from this project.
- Do not add third-party package references unless explicitly approved.
- Do not add UI framework references such as WPF, Telerik, AvalonEdit, or Windows-specific UI assemblies.
- Do not add runtime-engine references such as `pythonnet`.
- Keep this project portable and usable by both UI and non-UI applications.

## Contract Design Rules

- Keep `IScriptBase` small and focused on properties common to all script implementations.
- Add members to `IScriptBase` only when they are genuinely shared by all script types and consumers.
- Do not add Python-only, UI-only, editor-only, docking-only, project-file-only, or engine-only members to `IScriptBase`.
- Prefer adding concrete behavior to `PythonScript` or `ScriptingEngine` when the behavior is not part of the generic script contract.
- Avoid methods on `IScriptBase` that would force a specific execution engine or runtime model.
- Keep contract names generic and stable.
- Treat public interface and enum changes as cross-project changes because they affect dependent projects and serialized/project-loaded data.

## Serialization and Compatibility

- Assume these contracts may be used by project XML/zip loading, UI binding, runtime execution, and future persistence.
- Be conservative when renaming public members.
- Be conservative when changing property types.
- Prefer additive changes over breaking changes.
- If a breaking contract change is required, update all dependent projects in the same change.

## C# Style

- Use namespace `Qenex.QSuite.Scripting.Script`.
- Nullable reference types are enabled.
- Use file-scoped namespaces.
- Keep files small and one concept per file.
- Keep abstractions clear and minimal.
- Avoid unrelated formatting cleanup.
- Remove unused `using` directives when editing a file, but do not reformat unrelated files.

## Verification

After meaningful changes, build this project:

```text
dotnet build Scripting/Script/Script.csproj
```

If public contracts changed, also build known dependent projects:

```text
dotnet build Scripting/PythonScript/PythonScript.csproj
dotnet build Scripting/ScriptingEngine/ScriptingEngine.csproj
dotnet build QInsight/QInsight.csproj
```

When changing enum members or public interface properties, search the repository for usages and update dependent code deliberately.
