# Qenex.QSuite.Scripting.PythonScript

## Scope

This file applies to the `Qenex.QSuite.Scripting.PythonScript` project.

It supplements:

- root `AGENTS.md`
- `Scripting/AGENTS.md`
- `Scripting/Script/AGENTS.md` for shared script contracts

## Purpose

`Qenex.QSuite.Scripting.PythonScript` defines the Python-specific script model for QSuite.

It connects the generic scripting contracts from `Qenex.QSuite.Scripting.Script` to a concrete Python script type that can be loaded, configured, displayed, and executed by other parts of the system.

This project should describe Python scripts as data and specification. It should not host Python, execute Python code, bind QSuite variables into Python, redirect Python logs, or provide UI/editor behavior.

## Project Role

This project is responsible for:

- Python-specific script model implementation.
- Python script defaults that are independent of the runtime host.
- Sample or default Python script files that belong with the Python script type.
- Keeping Python scripts compatible with the shared script abstraction layer.

This project is not responsible for:

- Python runtime initialization.
- `pythonnet` integration.
- Python scope management.
- Script scheduling or lifecycle execution.
- Variable binding between QSuite variables and Python objects.
- Logging redirection from Python into QSuite logs.
- QInsight script editor UI, syntax highlighting UI, docking panes, or settings views.
- Driver, protocol, module, or hardware behavior.

## Dependency Rules

Default dependency direction:

```text
PythonScript -> Script
ScriptingEngine -> Script
ScriptingEngine may consume PythonScript models through Script abstractions when needed
```

Rules:

- This project should depend on `Qenex.QSuite.Scripting.Script`.
- Do not add references to `ScriptingEngine` from this project.
- Do not add references to QInsight, WPF, Telerik, AvalonEdit, drivers, protocols, variables, logging, controls, or module projects unless explicitly approved.
- Do not add `pythonnet` or Python runtime hosting packages here.
- Keep concrete execution behavior in `ScriptingEngine`.
- Keep UI/editor behavior in UI projects such as `QInsight`.
- Avoid circular dependencies between scripting projects.

## Model Rules

- Keep Python script models aligned with the shared `IScriptBase` contract.
- Add Python-specific model data only when it is truly part of the Python script definition, not the execution host.
- Do not duplicate shared script metadata that already belongs in `Script`.
- Do not introduce engine-specific state that only makes sense while a script is running inside a particular host.
- Keep timing and state behavior compatible with how the engine and UI consume generic script contracts.
- Preserve simple construction and serialization-friendly properties unless a broader persistence change is planned.

## Python File Rules

- Treat files under `Scripts` as sample/default Python script content unless a feature explicitly defines them as packaged runtime assets.
- Keep sample scripts small and focused on demonstrating expected script shape.
- Avoid machine-specific paths, local environment assumptions, or required external packages in sample scripts.
- Do not put QSuite runtime host implementation in `.py` files.
- If sample scripts rely on names injected by the engine, keep that dependency obvious and documented near the sample or in consuming documentation.

## Boundary With ScriptingEngine

Runtime behavior belongs in `ScriptingEngine`.

Examples of engine-owned concerns:

- Python DLL/runtime configuration.
- Python GIL handling.
- Shared or isolated Python scopes.
- Script execution ordering.
- Startup, shutdown, periodic, manual, or event-triggered execution.
- Script output capture.
- Exception handling during execution.
- Variable bridges and bindings.

If a change requires any of those concerns, update `ScriptingEngine` rather than adding that behavior to this project.

## Boundary With Script

Shared contracts belong in `Script`.

If a change is useful for all script types, update the `Script` project deliberately and verify dependent projects.

If a change is only meaningful for Python scripts, keep it in `PythonScript`.

Do not put Python-specific assumptions into shared script contracts.

## C# Style

- Use namespace `Qenex.QSuite.Scripting.PythonScript`.
- Nullable reference types are enabled.
- Use file-scoped namespaces.
- Keep files small and focused.
- Prefer simple data/model code over framework-heavy behavior.
- Follow existing naming and property patterns.
- Do not perform broad formatting changes or unrelated cleanup.

## Verification

After meaningful changes, build this project:

```text
dotnet build Scripting/PythonScript/PythonScript.csproj
```

If shared script contracts or consumers are affected, also build:

```text
dotnet build Scripting/Script/Script.csproj
dotnet build Scripting/ScriptingEngine/ScriptingEngine.csproj
dotnet build QInsight/QInsight.csproj
```

When changing Python script model behavior, search for usages in the repository and check that project loading, UI binding, and engine execution still agree on the generic script contract.
