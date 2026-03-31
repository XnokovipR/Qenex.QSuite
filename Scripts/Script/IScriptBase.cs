namespace Qenex.QSuite.Scripts.Script;

public interface IScriptBase
{
    string FileName { get; set; }
    string Content { get; set; }

    ScriptExecutionMode ExecutionMode { get; set; }
    string AdditionalInfo { get; set; }

	double LastExecutionInterval { get; set; }
    double AverageExecutionInterval { get; set; }
    double MaxExecutionInterval { get; set; }
}