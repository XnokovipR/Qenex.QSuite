namespace Qenex.QSuite.Scripting.ScriptingEngine;

/// <summary>
/// Outcome of <see cref="ScriptingContext.GetStartDecision"/>: how a runtime/replay session
/// may start given the optional Python setting and the project's scripts.
/// </summary>
public enum ScriptingStartDecision
{
    /// <summary>Python is enabled; the scripting context is initialized and scripts run.</summary>
    PythonEnabled,

    /// <summary>Python is disabled and the project has no scripts; start silently.</summary>
    NoScripts,

    /// <summary>Python is disabled and every script is disabled for this mode; start with a warning.</summary>
    DisabledScriptsOnly,

    /// <summary>Python is disabled but a script is enabled for this mode; the session must not start.</summary>
    EnabledScriptsBlocked
}
