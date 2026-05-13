# Qenex.QSuite.Scripting.PythonScript

## Scope

This file applies to the `Qenex.QSuite.Scripting.PythonScript` project.

It supplements:
- root `AGENTS.md`
- `AGENTS.md` in the `Qenex.QSuite.Scripting` directory.

## Purpose

The `Qenex.QSuite.Scripting.PythonScript` project is responsible for defining specifications concerning Python scripts.


## Dependency Rules

The project should depend on the `Qenex.QSuite.Scripting.Script` project, 
as it implements the Python script based on the general script definitions and interfaces provided by the `Script` project.

Default dependency direction:
```
PythonSctipt -> Script
```