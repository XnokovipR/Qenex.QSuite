# Qenex.QSuite.Scripting.Script

## Scope

This file applies to the `Qenex.QSuite.Scripting.Script` project.

It supplements:
 - root `AGENTS.md`
 - `AGENTS.md` in the `Qenex.QSuite.Scripting` directory.

## Purpose

The `Qenex.QSuite.Scripting.Script` project is responsible for defining general script definitions, interfaces, and abstractions for different types of scripts. 
It provides the foundational structures and contracts that various script implementations, such as the Python script, will adhere to.

## Dependency rules

This project does not depend on concrete driver projects.
This project must remain dependency-light.
Do not add any dependencies to this project without explicit approval. This project should remain as independent as possible, serving as a core abstraction layer for scripting functionality.
