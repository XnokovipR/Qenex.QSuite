# Qenex.QSuite

## Scope
Applies to the entire Qenex.QSuite solution. More specific `AGENTS.md` files
supplement and override this one within their directory scope.

## Purpose
Qenex.QSuite is a modular, multi-project **C# (.NET 10)** solution for control,
logging, and visualization of real-time data to/from electronic devices
(ECUs, Raspberry Pi, Arduino, remote computers, …). It includes a **Python
scripting engine** for automating data processing and device control.

Runtime targets:
- Console application on Windows/Linux/macOS (in the future also as a service).
- WPF application on Windows — the `QInsight` project (visualization + device control).

## Critical rules (do not violate)
- The existing code works and is non-trivial. **Do not change working behavior,
  public contracts, or architecture without an explicit request.**
- Prefer small, local, additive changes. **No broad cross-solution refactoring.**
- Respect project boundaries: **do not move types between projects** and **do not
  add new cross-project dependencies** without checking the existing dependency
  direction. No circular dependencies.
- Keep hardware-, protocol-, scripting-, logging-, UI-, and domain layers
  separated unless a local `AGENTS.md` states otherwise.
- Only touch the target project, its directly related shared libraries, and
  explicitly referenced dependencies.

## Repository structure
Each top-level directory holds one or more C# projects and may have its own `AGENTS.md`.

- `Common` — shared primitives, abstractions, base types, utilities (incl. `PluginManager`).
- `Controls` — WPF UI controls and visual components.
- `Drivers` — hardware drivers, low-level communication, driver lifecycle.
- `Helpers` — shared utility functions.
- `LogSystems` — logging infrastructure (diagnostics, errors, runtime info).
- `Modules` — application orchestration; integrates drivers, protocols, variables,
  and project-config loading. Used by both console and WPF apps.
- `Protocols` — communication protocol implementations.
- `QInsight` — main WPF visualization and device-control application.
- `Scripting` — Python scripting engine integration and script execution.
- `Specifications` — shared specifications for protocols, devices, components.
- `Variables` — communication variables and runtime data structures.

## AGENTS.md hierarchy & precedence
Apply instructions in this order (more specific wins):
1. Explicit user request.
2. Nearest `AGENTS.md` (target directory, then parent directories).
3. This root `AGENTS.md`.
4. Existing code style and patterns.

Before modifying code, check for a more specific `AGENTS.md` in the target and
project directories. Resolve conflicts toward the more specific rule; ask only
when a conflict cannot be resolved safely.

## Conventions
- Match the structure, naming, and design of nearby code; reuse existing
  abstractions, helpers, converters, and patterns instead of adding parallel ones.
- Namespaces: `Qenex.QSuite.<Category>.<Project>`; file-scoped namespaces;
  nullable enabled; implicit usings.
- Follow existing conventions for access modifiers, async naming, DI style,
  logging, and exception handling.
- Use the modern C# already used in the project; do not introduce newer language
  features unless the target framework and surrounding style clearly support them.
- Do not reformat unrelated code or do mechanical cleanup outside the requested scope.
