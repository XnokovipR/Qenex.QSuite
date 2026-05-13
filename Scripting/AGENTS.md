# Qenex.QSuite.Scripting

## Scope

This file applies tp all projects within the `Qenex.QSuite.Scripting` directory and its subdirectories.

It supplements the root `AGENTS.md`.

Project-local `AGENTS.md` files may add more specific rules for individual projects.

## Purpose

The projects within the `Qenex.QSuite.Scripting` directory are responsible for implementing the scripting engine, script execution, and individual scripts such as the python script.

## Projects

- `Qenex.QSuite.Scripting.ScriptingEngine`: Core scripting Python engine, script execution logic, binding of C# variables and functions to the scripting environment, and management of script lifecycle.
- `Qenex.QSuite.Scripting.Script`: General script definitions, interfaces, and abstractions for different types of scripts.
- `Qenex.QSuite.Scripting.Python`: Implementation of the Python script.

## Dependency Rules

Default dependency direction:
```
PythonSctipt -> Script
```
