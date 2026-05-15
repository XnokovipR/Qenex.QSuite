# Qenex.QSuite.Scripting

## Scope

This file applies to all projects within the `Qenex.QSuite.Scripting` directory and its subdirectories.

It supplements the root `AGENTS.md`.

Project-local `AGENTS.md` files may add more specific rules for individual projects. Check them before modifying files under:

- `Script`
- `PythonScript`
- `ScriptingEngine`

## Purpose

The projects within `Qenex.QSuite.Scripting` define the scripting model and runtime used by QSuite.

This area is responsible for:

- Generic script contracts and script metadata.
- Concrete script types, currently Python scripts.
- Script execution lifecycle.
- Python runtime hosting through `pythonnet`.
- Binding QSuite variables into script-accessible objects.
- Redirecting script output into the QSuite logging system.

The scripting layer should let applications such as QInsight load, edit, configure, and execute scripts without putting scripting engine internals into the UI project.

## Projects

- `Qenex.QSuite.Scripting.Script`
  - Defines the script abstraction layer.
  - Contains `IScriptBase` and common enums such as `ScriptExecutionMode`, `ScriptFileType`, and `ScriptRunState`.
  - Must remain dependency-light and independent from concrete scripting engines, UI projects, drivers, protocols, and hardware-facing code.

- `Qenex.QSuite.Scripting.PythonScript`
  - Defines the Python script implementation.
  - Contains `PyScript`, which implements `IScriptBase`.
  - Contains sample/default Python script files under `Scripts`.
  - Depends on `Script`.

- `Qenex.QSuite.Scripting.ScriptingEngine`
  - Hosts script execution.
  - Uses `pythonnet` and `Python.Runtime`.
  - Creates and disposes shared Python scopes.
  - Binds QSuite variables to Python names through `VariableBinding` and `VariableBridge`.
  - Redirects Python `stdout` and `stderr` into `ILogger` through `PythonLogWriter`.
  - Depends on `Script`, `Variables/QVariables`, and `LogSystems/LogSystem`.

## Dependency Rules

Default dependency direction:

```text
PythonScript -> Script
ScriptingEngine -> Script
ScriptingEngine -> Variables/QVariables
ScriptingEngine -> LogSystems/LogSystem
```

Rules:

- `Script` must not reference `PythonScript`, `ScriptingEngine`, QInsight, drivers, protocols, variables, logging, or UI projects unless explicitly approved.
- `PythonScript` should reference only `Script` unless there is a clear, approved reason to add another dependency.
- `ScriptingEngine` may reference `Script`, variable abstractions, and logging because it bridges runtime scripts to QSuite data and diagnostics.
- UI projects should consume scripting contracts and engine behavior; scripting projects should not reference UI projects.
- Do not add circular dependencies between scripting projects.
- Do not move generic script contracts into concrete script or engine projects.
- Do not put Python-specific implementation details in `Script`.

## Responsibility Boundaries

Keep responsibilities separated:

- Script definitions and execution metadata belong in `Script`.
- Python script data models belong in `PythonScript`.
- Runtime hosting, Python scope management, variable binding, execution scheduling, and log redirection belong in `ScriptingEngine`.
- Script editor UI, syntax highlighting UI, docking panes, and user interactions belong in UI projects such as `QInsight`.
- Driver/protocol/device behavior belongs in the corresponding driver and protocol projects, not in scripting.

When adding functionality, first decide whether it is:

- a script contract,
- a specific script type,
- engine/runtime behavior,
- UI/editor behavior,
- or device/module behavior.

Place the code accordingly.

## Script Contracts

- Keep `IScriptBase` focused on properties common to all script types.
- Add new script lifecycle states or execution modes only when they are meaningful for more than one script implementation or explicitly part of the shared contract.
- Keep shared enums stable because they may be serialized, displayed, or used by project loading code.
- Avoid adding runtime-engine-specific members to `IScriptBase`.
- Avoid adding UI-only members such as selected state, editor state, colors, panes, or view options to script contracts.

## Python Script Rules

- `PyScript` should remain a concrete data model for Python scripts.
- Keep Python-specific defaults in `PythonScript`, not in `Script`.
- Preserve the relationship between `PyScript` and `IScriptBase`.
- Treat files under `PythonScript/Scripts` as default/sample script content unless a feature explicitly makes them runtime assets.
- Do not place Python runtime hosting or `pythonnet` code in `PythonScript`; that belongs in `ScriptingEngine`.

## Scripting Engine Rules

- Python runtime initialization must be centralized and guarded. Do not initialize Python independently from multiple places.
- Use `Python.Runtime.Py.GIL()` around Python object access and script execution.
- Keep Python execution serialized unless the engine is deliberately redesigned for safe concurrent execution.
- Preserve the shared-scope model unless a change explicitly requires isolated scopes.
- Dispose Python scopes and restore redirected `sys.stdout` / `sys.stderr` when the context is disposed.
- Route script output and script execution errors through `ILogger`.
- Do not write directly to console output for engine diagnostics.
- Avoid hard-coded machine-specific runtime paths in new code. Prefer `ScriptEngineSettings` or caller-provided configuration for Python DLL/runtime paths.
- Be careful with fire-and-forget execution. Long-running script execution should have a clear lifecycle, logging, and cancellation story before being added.

## Variable Binding Rules

- Keep variable binding code in `ScriptingEngine`.
- `VariableBinding` should represent the mapping between script-visible names and QSuite variable IDs.
- `VariableBridge` should remain a narrow adapter over `IVariableBase`.
- Use existing `IVariableBase.GetValue()` and `IVariableBase.SetValue(...)` behavior instead of duplicating variable storage.
- Validate script-visible variable names before exposing them to Python if new naming sources are introduced.
- Do not expose more of the variable object graph to scripts than is intentionally supported.

## Logging and Errors

- Use `ILogger` for engine diagnostics and script output.
- Log Python execution failures with enough context to identify the script file.
- Keep script `RunState` and execution duration metrics consistent when adding execution paths.
- Do not swallow Python exceptions silently.
- Avoid throwing raw Python/runtime exceptions across higher-level application boundaries when a logged script failure is the intended behavior.

## Configuration

- Engine configuration belongs in `ScriptEngineSettings` or caller-provided setup.
- Settings that are specific to a UI application belong in that UI project, not here.
- Prefer configurable Python runtime paths over local absolute paths.
- Keep defaults portable across developer machines and target operating systems unless the project explicitly requires a platform-specific behavior.

## C# Style

- Follow existing namespaces:
  - `Qenex.QSuite.Scripting.Script`
  - `Qenex.QSuite.Scripting.PythonScript`
  - `Qenex.QSuite.Scripting.ScriptingEngine`
- Nullable reference types are enabled.
- Use file-scoped namespaces.
- Keep code small and explicit.
- Prefer existing interfaces and project patterns over new abstractions.
- Do not perform broad formatting changes or unrelated cleanup.
- Keep public contracts stable and minimal.

## Tests and Verification

- Build the relevant scripting project after meaningful changes when possible:

```text
dotnet build Scripting/Script/Script.csproj
dotnet build Scripting/PythonScript/PythonScript.csproj
dotnet build Scripting/ScriptingEngine/ScriptingEngine.csproj
```

- Build dependent projects when changing shared contracts.
- For engine changes, verify behavior with at least a small script that exercises:
  - startup or manual execution,
  - Python log output,
  - variable get/set binding,
  - and Python exception logging.
- If Python runtime configuration is required, document what was assumed or why verification could not be completed.

## Local Notes

- There is no project-local `AGENTS.md` currently under `ScriptingEngine`; this file provides the specific guidance for that project.
- More specific `AGENTS.md` files under `Script` and `PythonScript` override this file within their scopes.
