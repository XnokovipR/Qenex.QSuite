# AGENTS.md

## Scope

This file defines repository-wide instructions for Codex and other AI coding agents.

Each top-level directory contains either subdirectories or a separate C# project and may include its own `AGENTS.md`.
The subdirecotries contain either other subdirectories or a separate C# project and may include its own `AGENTS.md`.
Project-local `AGENTS.md` files override this root file for files inside their directory tree.

Top-level directories:

- `Common`
- `Controls`
- `Drivers`
- `Helpers`
- `LogSystems`
- `Modules`
- `Protocols`
- `QInsight`
- `Scripting`
- `Specifications`
- `Variables`

## Repository Model

This repository is a multi-project C# solution.

Assume that projects are intentionally separated by responsibility. Do not move types between projects unless the change is explicitly requested or clearly required by dependency direction.

Prefer small, localized changes over broad refactoring.

## Agent Precedence

When editing files, apply instructions in this order:

1. Explicit user request.
2. Nearest `AGENTS.md` in the file's directory or parent directories.
3. This root `AGENTS.md`.
4. Existing code style and patterns.

If instructions conflict, follow the more specific instruction.

## General C# Rules

Use modern C# where it is already used by the project, but do not introduce newer language features if the target framework or project style does not clearly support them.
Use clear code, SOLID principles, design patters, depency injection and other software rules used by experienced senior software developers. 

Follow existing conventions for:

- namespaces
- file layout
- nullable annotations
- access modifiers
- async naming
- dependency injection style
- logging style
- exception handling
- test structure

Do not reformat unrelated code.

Do not perform mechanical cleanup outside the requested scope.

Avoid speculative abstractions. Add interfaces, factories, base classes, or generic layers only when there is an immediate concrete need.

ZDE JSEM SKONCIL -- kontrola
## Architecture Rules

Respect project boundaries.

Do not create new cross-project dependencies without checking existing dependency direction.

Prefer dependencies toward lower-level/shared projects such as `Common` or `Helpers` only when the abstraction is genuinely reusable.

Avoid circular dependencies.

Hardware-facing, protocol-facing, scripting, logging, UI, and domain modules should remain separated unless the relevant project-local `AGENTS.md` says otherwise.

## Expected Directory Responsibilities

These descriptions are repository-wide defaults. Local `AGENTS.md` files may refine them.

### `Common`

Shared primitives, common abstractions, base types, result models, constants, and low-level utilities used by multiple projects.

Keep this project stable and dependency-light.

### `Controls`

UI controls, visual components, view-related helpers, and control-specific behavior.

Avoid placing business logic, protocol logic, or driver logic here.

### `Drivers`

Hardware/device drivers, low-level device communication, hardware abstraction, and driver lifecycle code.

Be conservative with timing, resource handling, disposal, retries, and error propagation.

### `Helpers`

Utility functions and implementation helpers.

Do not turn this project into a dumping ground. Prefer domain-specific helpers inside the owning project unless they are broadly reusable.

### `LogSystems`

Logging infrastructure, sinks, formatters, log routing, log persistence, and diagnostic output.

Do not silently swallow exceptions unless the existing logging design explicitly does so.

### `Modules`

Application modules, feature modules, orchestration units, and higher-level functional blocks.

Keep module boundaries explicit. Avoid hidden coupling through globals or service locators.

### `Protocols`

Communication protocols, encoders, decoders, parsers, serializers, framing, checksums, and protocol state machines.

Prioritize deterministic behavior, validation, and clear error reporting.

### `QInsight`

Main application, composition root, product-specific orchestration, or high-level integration layer.

Avoid placing reusable low-level logic here if it belongs in a lower-level project.

### `Scripting`

Script execution, script APIs, script bindings, sandboxing, automation hooks, and runtime extensibility.

Be careful with security, reflection, dynamic execution, and user-provided code.

### `Specifications`

Specifications, contracts, schemas, protocol descriptions, generated definitions, test vectors, or formal documentation used by the codebase.

Do not casually edit generated or externally sourced specification files.

### `Variables`

Variable models, typed values, variable storage, binding, validation, metadata, change notification, and runtime variable access.

Preserve type safety and validation rules.

## Dependency Guidance

Default dependency direction should be approximately:

```text
QInsight
  -> Modules
      -> Protocols / Drivers / Variables / LogSystems
          -> Common / Helpers
Controls
  -> Common / Helpers
Scripting
  -> Modules / Variables / Common
Specifications
  -> usually no runtime dependencies