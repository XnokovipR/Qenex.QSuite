namespace Qenex.QSuite.Scripting.Script;

public interface IScriptBase
{
    string FileName { get; set; }
    string Content { get; set; }

    ScriptExecutionMode ExecutionMode { get; set; }
    string AdditionalInfo { get; set; }

    bool IsEnabled { get; set; }
    bool IsReplayEnabled { get; set; }
    ScriptRunState RunState { get; set; }
	double LastExecutionDurationMs { get; set; }
    double AverageExecutionDurationMs { get; set; }
    double MaxExecutionDurationMs { get; set; }
    
}