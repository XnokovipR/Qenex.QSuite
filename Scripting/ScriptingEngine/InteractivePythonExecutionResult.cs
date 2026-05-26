namespace Qenex.QSuite.Scripting.ScriptingEngine;

public class InteractivePythonExecutionResult
{
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public bool IsIncomplete { get; set; }
}
