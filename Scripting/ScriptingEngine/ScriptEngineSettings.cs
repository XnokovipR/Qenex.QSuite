namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class ScriptEngineSettings
{
    /// <summary>
    /// Python is optional: when false the Python runtime is never loaded, project scripts do
    /// not run and the interactive interpreter is unavailable. The DLL path is kept but ignored.
    /// </summary>
    public bool UsePythonScripts { get; set; }

    public string PythonDllPath { get; set; } = string.Empty;
}
