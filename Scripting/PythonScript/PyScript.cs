using Qenex.QSuite.Scripting.Script;

namespace Qenex.QSuite.Scripting.PythonScript;

public class PyScript : IScriptBase
{
    private int executionCount;
    
    public ScriptFileType ScriptType => ScriptFileType.Python;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public ScriptExecutionMode ExecutionMode { get; set; } = ScriptExecutionMode.Manual;
	public string AdditionalInfo { get; set; } = string.Empty;

	public double LastExecutionInterval
    {
        get => field;
        set
        {
            field = value;
            // Update the average  & max execution interval
            if (executionCount == 0)
            {
                AverageExecutionInterval = value;
                MaxExecutionInterval = value;
            }
            else
            {
                AverageExecutionInterval = ((AverageExecutionInterval * executionCount) + value) / (executionCount++);
            }
            
            if (value > MaxExecutionInterval)
            {
                MaxExecutionInterval = value;
            }
        }
    }
    public double AverageExecutionInterval { get; set; }
    public double MaxExecutionInterval { get; set; }
}