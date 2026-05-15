# Qenex.QSuite.Scripting.ScriptingEngine

## Scope

This file applies to the `Qenex.QSuite.Scripting.ScriptingEngine` project.

It supplements:

- root `AGENTS.md`
- `Scripting/AGENTS.md`
- `Scripting/Script/AGENTS.md` for shared script contracts

## Purpose

`Qenex.QSuite.Scripting.ScriptingEngine` hosts and executes scripts for QSuite.

The project connects shared script definitions to runtime execution by hosting Python through `pythonnet`, managing Python scopes, binding QSuite variables into scripts, and forwarding script output to the QSuite logging system.

## Project Role

This project is responsible for:

- Python runtime hosting.
- Script execution lifecycle.
- Shared Python scope creation and disposal.
- Startup, shutdown, and other execution-mode handling.
- Binding QSuite variables into script-accessible objects.
- Redirecting Python output and errors into `ILogger`.
- Engine-level settings needed to configure script execution.

This project is not responsible for:

- Defining generic script contracts. Those belong in `Scripting/Script`.
- Defining Python script data models. Those belong in `Scripting/PythonScript`.
- Script editor UI, syntax highlighting UI, docking panes, or user interaction.
- Driver, protocol, module, or hardware implementation.
- Reusable UI controls or application-specific settings.

## Dependency Rules

Current intended dependency direction:

```text
ScriptingEngine -> Script
ScriptingEngine -> Variables/QVariables
ScriptingEngine -> LogSystems/LogSystem
```

Rules:

- Keep dependencies limited to script contracts, variable abstractions, logging, and Python runtime hosting.
- Do not reference QInsight, WPF, Telerik, AvalonEdit, controls, drivers, protocols, or module projects unless explicitly approved.
- Do not move shared script contracts into this project.
- Do not create dependencies from `Script` or `PythonScript` back to `ScriptingEngine`.
- Avoid circular dependencies.

## Runtime Rules

- Keep Python runtime initialization centralized.
- Guard initialization so Python is not initialized multiple times accidentally.
- Use `Py.GIL()` for Python runtime access.
- Keep script execution serialized unless the engine is deliberately redesigned for concurrent execution.
- Prefer configurable Python runtime paths through engine settings instead of hard-coded local paths.
- Do not write engine diagnostics directly to console output.
- Route script output, warnings, and failures through `ILogger`.

## Scope Lifecycle Rules

- Create script scopes in the engine, not in UI or model projects.
- Dispose Python scopes when execution context is no longer needed.
- Restore redirected Python `stdout` and `stderr` when disposing the scope.
- Keep startup and shutdown script behavior near the scope lifecycle logic.
- Be careful with fire-and-forget execution; long-running scripts need clear logging, lifecycle, and cancellation behavior.

## Variable Binding Rules

- Keep variable binding code in this project.
- Use QSuite variable abstractions instead of duplicating variable storage.
- Keep script-visible variable wrappers narrow and intentional.
- Do not expose more of the variable object graph than scripts need.
- Validate or normalize script-visible names if new naming sources are introduced.

## Script Execution Rules

- Execute scripts through the shared `IScriptBase` contract.
- Keep engine logic independent from concrete UI view models.
- Log script failures with enough context to identify the script.
- Keep run state and execution metrics consistent when adding new execution paths.
- Avoid silently swallowing Python exceptions.

## Configuration

- Put engine-specific configuration in engine settings.
- Keep UI-specific settings in UI projects.
- Avoid machine-specific defaults in new code.
- Document any required local Python runtime assumption when verification depends on it.

## C# Style

- Use namespace `Qenex.QSuite.Scripting.ScriptingEngine`.
- Nullable reference types are enabled.
- Use file-scoped namespaces.
- Keep implementation focused and explicit.
- Follow existing region and file organization when editing existing files.
- Add comments only where Python interop or threading behavior would otherwise be unclear.
- Do not perform unrelated formatting cleanup.

## Verification

After meaningful changes, build this project:

```text
dotnet build Scripting/ScriptingEngine/ScriptingEngine.csproj
```

If shared script contracts or consumers are affected, also build:

```text
dotnet build Scripting/Script/Script.csproj
dotnet build Scripting/PythonScript/PythonScript.csproj
dotnet build QInsight/QInsight.csproj
```

For runtime changes, verify with a small script when possible. Good checks include script output logging, variable get/set binding, startup or shutdown execution, and Python exception logging.
