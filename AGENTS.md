# Qenex.QSuite

## Scope

This file applies to the entire Qenex.QSuite repository and all its projects.

## Purpose

Qenex.QSuite is a multi-project C# solution for control, logging, and visualization of real-time data from and to electronic devices
such as ECUs, Raspberry Pi, Arduino, remote computers and other similar systems.

The solution also includes a scripting engine for automating operations. 
It will allow users to create python scripts to automate operations, data processing, and device control.

The Qenex.QSuite is designed to be modular, maintainable, and extensible.

The repository contains projects for:

- communication protocols
- hardware drivers
- logging systems
- application modules
- scripting
- visualization
- shared abstractions and infrastructure

The solution can run:

- as a console application on Windows, Linux, and macOS
- in the future also as a service
- as a WPF application on Windows (QInsight project)

The WPF application (QInsight) is used for visualization of real-time data and control of connected devices.

The console application is used for backend logic without UI, for example:

- automated testing
- data processing
- headless operation

## Repository structure

Each top-level directory may contain one or more C# projects, subdirectories, and optionally its own `AGENTS.md`.

Project-local `AGENTS.md` files supplement and override this root file within their directory scope.

Top-level directories:

- `Common`
    - Shared primitives, abstractions, base types, and universal utilities.
    - Includes infrastructure such as `PluginManager`.

- `Controls`
    - WPF UI controls and visual components.

- `Drivers`
    - Hardware drivers, low-level communication, hardware abstraction, and driver lifecycle logic.

- `Helpers`
    - Shared utility functions and implementation helpers.

- `LogSystems`
    - Logging infrastructure for diagnostics, errors, and runtime information.

- `Modules`
    - Main application orchestration layer.
    - Integrates:
        - drivers
        - protocols
        - variables
        - project configuration loading
    - Used by both console and WPF applications.

- `Protocols`
    - Communication protocol implementations.

- `QInsight`
    - Main WPF visualization and device-control application.

- `Scripting`
    - Python scripting engine integration and script execution infrastructure.

- `Specifications`
    - Shared specifications for protocols, devices, and related components.

- `Variables`
    - Communication variables and runtime data structures.

## Repository boundaries

This repository is a multi-project C# solution.

Projects are intentionally separated by responsibility.

- Do not move types between projects unless explicitly requested or clearly required by dependency direction.
- Do not create new cross-project dependencies without checking existing dependency direction.
- Avoid circular dependencies.
- Prefer small and localized changes over broad refactoring.
- Do not analyze or modify unrelated projects.

Limit changes to:
- the target project
- directly related shared libraries
- explicitly referenced dependencies

Avoid broad cross-solution refactoring unless explicitly requested.

Hardware-facing, protocol-facing, scripting, logging, UI, and domain modules should remain separated unless the relevant project-local `AGENTS.md` states otherwise.

## AGENTS.md hierarchy

This root `AGENTS.md` contains only global rules and repository structure information.

Before modifying code:
- always check for a more specific `AGENTS.md`
- check both the target directory and project directory

More specific `AGENTS.md` files supplement and override rules from parent directories within their scope.

## Agent precedence

When editing files, apply instructions in this order:

1. Explicit user request.
2. Nearest `AGENTS.md` in the file's directory or parent directories.
3. This root `AGENTS.md`.
4. Existing code style and patterns.

If instructions conflict, follow the more specific instruction. Ask for clarification only when the conflict cannot be resolved safely.

## Code continuity rules

Generated or modified code must follow the existing code structure, style, naming, architecture, and design logic.

Before suggesting or changing code:

- Inspect nearby existing classes, interfaces, methods, and tests.
- Reuse existing abstractions, patterns, helper classes, factories, converters, and naming conventions.
- Prefer extending the current design over introducing a new parallel design.
- Do not introduce new architectural patterns, frameworks, dependency injection styles, or abstractions unless explicitly requested.
- Avoid unnecessary refactoring.
- Prefer minimal and localized changes.
- Do not replace established architecture unless explicitly requested.
- Keep changes consistent with surrounding projects and modules.
- When adding new code, place it where similar functionality already exists.
- Avoid creating duplicate utilities, helpers, converters, or abstractions when equivalent functionality already exists.

## General C# rules

Use modern C# already used by the project, but do not introduce newer language features unless the target framework 
and existing project style clearly support them.

Write clean, maintainable code following principles already used by the solution:
- SOLID principles
- dependency injection
- established design patterns
- separation of responsibilities

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

Do not:
- reformat unrelated code
- perform mechanical cleanup outside the requested scope
- introduce speculative abstractions
- add interfaces, factories, base classes, or generic layers without immediate concrete need