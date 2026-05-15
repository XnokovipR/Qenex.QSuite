# Qenex.QSuite.QInsight

## Scope

This file applies to the entire `Qenex.QSuite.QInsight` project.

## Purpose

`Qenex.QSuite.QInsight` is the main WPF visualization and device-control application.

It allows users to view, control, edit, and log data from electronics devices such as ECUs, Raspberry Pi devices, Arduinos, remote computers, sensor devices, and other similar systems.

The project is built on `.NET 10`, WPF, Telerik UI for WPF, AvalonEdit, and Qenex UI libraries.

QInsight is a UI shell over the shared QSuite modules. It should coordinate and present data from drivers, protocols, variables, scripting, logging, controls, and project files.
It should not become the owner of hardware, protocol, logging, scripting, or module-domain logic that belongs in the corresponding shared projects.

## Project Responsibilities

QInsight is responsible for:

- WPF application startup and shell window composition.
- Telerik docking layout and pane/view-model orchestration.
- Ribbon commands and user-facing application actions.
- Solution Explorer presentation of loaded project/module data.
- Workspace documents for visual dashboards and control placement.
- Script editing views and script-related UI panels.
- Properties, logs, and script logs panels.
- UI-specific drag-and-drop behavior.
- UI-specific application settings such as window style, theme colors, and font size.
- Loading project zip files into UI-facing project data models.
- Connecting UI actions to shared module, driver, protocol, variable, logging, and scripting services.

QInsight is not responsible for:

- Implementing hardware drivers.
- Implementing communication protocols.
- Implementing core logging infrastructure.
- Implementing module XML parsing beyond UI-facing project loading coordination.
- Implementing reusable WPF controls that belong under `Controls`.
- Implementing generic helper or infrastructure code that belongs in `Common` or `Helpers`.
- Implementing scripting engine internals.

## Architecture

The project follows a WPF MVVM-style structure with Telerik docking as the main shell.

- `App.xaml` defines application-level Telerik theme resources and docking pane styles.
- `Views/ShellWindow.xaml` is the main window and owns the ribbon and `RadDocking` layout.
- `ViewModels/ShellWindowModel.cs` is the main shell view model.
- `ViewModels/ShellWindowModel.Commands.cs` contains shell and ribbon command creation and handlers.
- `ViewModels/ShellWindowModel.EventAggregator.cs` contains cross-view-model message subscriptions.
- `ViewModels/ViewModelBase.cs` is the base for docked tool panes.
- `ViewModels/WorkspaceViewModelBase.cs` is the base for document/workspace panes.
- `ViewModels/SolutionExplorerWrappers` contains tree node wrappers for project/module objects.
- `EventAggregatorMsgs` contains small message types used for UI view-model communication.

Prefer extending these existing structures over introducing a new UI framework, shell pattern, or messaging pattern.

## Directory Guide

- `AppConfig`
  - UI application settings, theme/design settings, and window state models.
  - Keep this limited to QInsight-specific settings.

- `Converters`
  - WPF value converters used by QInsight XAML.
  - Keep converters small, stateless where possible, and UI-specific.

- `DragDrop`
  - Telerik/WPF drag-and-drop behaviors for controls, workspaces, and variables.
  - Put behavior code here instead of in view code-behind when it can be expressed as a reusable behavior.

- `EventAggregatorMsgs`
  - Message DTOs for communication through `EventAggregator`.
  - Keep messages small, focused, and named by the UI event or intent they represent.

- `Helpers`
  - QInsight-only helper code.
  - Before adding helpers here, check whether an equivalent helper already exists in `Common`, `Helpers`, or Qenex UI libraries.

- `Icons`
  - Application and menu icons included as WPF resources.
  - Update `QInsight.csproj` resource entries when adding icons.

- `Models`
  - UI-facing data models and wrappers.
  - `Models/Project` handles QInsight project zip loading and conversion to runtime project data.
  - Do not put shared domain models here if they are needed outside QInsight.

- `Resources`
  - Embedded resources such as AvalonEdit syntax highlighting files.
  - Keep resource build actions consistent with `QInsight.csproj`.

- `ViewModels`
  - View models for shell panes, documents, tool windows, properties, logs, scripts, and solution explorer items.
  - Keep UI state, commands, and presentation logic here.

- `Views`
  - WPF XAML views and minimal code-behind.
  - Code-behind should be limited to view construction, WPF/Telerik interop that is difficult to bind, or view-only concerns.

## MVVM and Docking Rules

- New docked tool panes should generally derive from `ViewModelBase`.
- New document/workspace panes should generally derive from `WorkspaceViewModelBase`.
- Every docked pane view model must define stable `Header`, `Name`, `DockPosition`, and `IsDocument` values.
- Use existing `DockingPosition` values and the `TelerikDockingPanesFactory` integration instead of manually creating panes where possible.
- Add pane view models to `ShellWindowModel.ViewModels` when they should appear in the docking layout.
- Use `IsHidden` for pane visibility behavior consistent with existing panes.
- Keep docking layout concerns in the shell and pane metadata; do not scatter docking-specific logic across unrelated view models.

## Views and XAML

- Use Telerik controls consistently with the existing shell, ribbon, docking, diagram, grid, dialog, and input patterns.
- Use `Microsoft.Xaml.Behaviors.Wpf` triggers and commands for event-to-command bindings where the project already does so.
- Keep XAML namespaces, design-time `d:DataContext`, and `mc:Ignorable="d"` conventions consistent with nearby views.
- Prefer binding to view-model properties over code-behind state.
- Keep view code-behind minimal and avoid placing domain or workflow logic in `.xaml.cs` files.
- Use application/theme resources from `App.xaml`, `ShellWindow`, and `AppConfig` before introducing new local styling.
- Keep icon paths and resource usage consistent with existing ribbon and solution explorer icons.

## Commands and Events

- Use existing `RelayCommand<T>` and `RelayCommandAsync<T>` command types from Qenex UI libraries.
- Shell-level commands belong in `ShellWindowModel.Commands.cs`.
- Cross-pane communication should use the existing `EventAggregator`.
- Add new event aggregator message types under `EventAggregatorMsgs`.
- Keep message payloads simple and UI-focused.
- Prefer explicit command methods over large inline lambdas, except for very small view-only actions.
- Async command handlers should return `Task` and should handle/log exceptions where user-facing operations can fail.

## Project Loading and Runtime Data

- Project zip loading belongs in `Models/Project/ProjectZip.cs` and related project model classes.
- Runtime module creation should continue to use `XmlModuleHandler`, `UnifiedModuleFactory`, and plugin details from the shared projects.
- Plugin discovery in QInsight should remain UI orchestration only; plugin loader and plugin contract logic belongs in shared projects.
- Use the existing `Logger`/`ILogger` and publish log messages to UI log panes instead of writing directly to console output or message boxes for recoverable workflow errors.
- Be careful with project-file paths, script file names, and zip entries. Prefer structured APIs such as streams, XML serialization, and project-file helpers already used by the solution.

## Solution Explorer Rules

- Solution Explorer tree data is represented by wrappers under `ViewModels/SolutionExplorerWrappers`.
- Add a new wrapper type when a new project/module item needs distinct label, icon, children, or interaction behavior.
- Keep tree-building logic in `SolutionExplorerViewModel`.
- Preserve the existing wrapper pattern for drivers, protocols, variables, presentations, scripts, variable events, workspaces, and namespace/folder nodes.
- Double-click and click behavior should publish focused messages through `EventAggregator` instead of directly manipulating unrelated panes.

## Workspace and Control Rules

- Workspace visual editing is based on Telerik `RadDiagram`.
- Workspace-level UI state belongs in `WorkspaceViewModel`.
- Drag/drop behavior belongs under `DragDrop`.
- Controls placed on a workspace should continue to use control view models and views from the shared `Controls` projects.
- Do not duplicate reusable control implementations inside QInsight.
- When binding variables to controls, use the existing control interfaces and drag/drop payload conventions.
- Keep theme propagation for workspace controls consistent with `ShellWindow` and `MainAppSettings.Design`.

## Scripting UI Rules

- Script editor panes use `ScriptViewModel`, `ScriptWrapper`, AvalonEdit `TextDocument`, and embedded Python syntax highlighting resources.
- Keep script editor UI logic in QInsight, but keep script execution and scripting engine behavior in the `Scripting` project.
- When editing script-related UI, preserve synchronization between the AvalonEdit document and `ScriptWrapper.Content`.
- Keep script logs separate from general application logs when the UI already distinguishes them.

## Settings and Theme Rules

- QInsight settings are XML-serialized through `AppSettings` and related `AppConfig` models.
- Preserve compatibility with existing `QInsightAppSettings.xml` shape when changing settings.
- Add default values in `AppSettings.GetDefaultAppSettings`.
- Store WPF `Color` values using the existing XML string property pattern unless replacing the pattern is explicitly requested.
- Use `ApplicationTheme`, `DesignManager`, and `ShellWindow` theme helpers for dark/light UI decisions.

## Dependency Boundaries

- Keep QInsight dependent on shared projects, not the other way around.
- Do not add references from shared projects back to QInsight.
- Before adding a new project reference, check whether the dependency belongs in an existing shared abstraction instead.
- Avoid moving shared behavior into QInsight just because the UI is the current caller.
- Do not introduce new third-party UI libraries unless explicitly requested.
- Prefer existing Qenex libraries, Telerik WPF controls, and AvalonEdit where they already solve the problem.
- Use Telerik WPF controls before standard Microsoft WPF controls and other 3rd-party controls.

## C# Style

- Follow the existing namespace style: `Qenex.QInsight...`.
- Nullable reference types are enabled; avoid suppressions unless the lifecycle is clear and matches nearby code.
- Use existing file-scoped namespaces.
- Preserve the project’s current region-heavy organization in files that already use regions.
- Use `ObservableCollection<T>` for UI collections that are bound to WPF controls.
- Raise `OnPropertyChanged()` from setters for bindable properties, matching existing `PropertyChangedBaseWithValidation` usage.
- Keep public view-model properties bindable and avoid exposing mutable fields unless matching a nearby established pattern.
- Do not perform broad formatting changes or unrelated cleanup.

## Error Handling and Logging

- User-facing recoverable errors should be logged through the existing logging/event aggregator flow.
- Use Telerik dialogs for UI confirmations and modal UI where the project already does.
- Avoid swallowing exceptions silently unless the surrounding code has an intentional, narrow UI reason.
- Do not let background runtime operations crash the UI thread.
- Be careful with fire-and-forget runtime calls. If changing those flows, consider cancellation, logging, and user feedback.

## Tests and Verification

- Build the project after meaningful code changes when possible.
- Prefer:
  - `dotnet build QInsight/QInsight.csproj`
  - or a targeted solution build when cross-project changes require it.
- For UI changes, also inspect the relevant XAML and view-model binding paths manually.
- If changing project loading, verify with the sample project under `_TestProject` when practical.
- Do not rely on WPF UI automation as the only verification unless the requested change is specifically UI behavior.

## Local Artifacts

- Treat `bin` and `obj` as generated output.
- Do not edit generated build output.
- Treat `_TestProject` as local test/sample data unless explicitly asked to change it.
- Treat `_Doc` as project notes/documentation; update it only when the request is documentation-related.
