using Qenex.QSuite.Scripting.Script;

namespace Qenex.QSuite.Scripting.PythonScript;

public class PyScript : IScriptBase
{
    private int executionCount;

    public ScriptFileType ScriptType => ScriptFileType.Python;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public string AdditionalInfo { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsReplayEnabled { get; set; }
    public ScriptExecutionMode ExecutionMode { get; set; } = ScriptExecutionMode.Manual;
    public bool Blocking { get; set; } = true;
    public double TimeoutMs { get; set; }
    public ScriptRunState RunState { get; set; }

    public double LastExecutionDurationMs
    {
        get;
        set
        {
            if (Math.Abs(field - value) < 1e-9) return;

            field = value;

            // Update the average & max execution interval
            if (executionCount == 0)
            {
                AverageExecutionDurationMs = value;
                MaxExecutionDurationMs = value;
            }
            else
            {
                AverageExecutionDurationMs = ((AverageExecutionDurationMs * executionCount) + value) / (executionCount + 1);
            }

            executionCount++;

            if (value > MaxExecutionDurationMs)
            {
                MaxExecutionDurationMs = value;
            }
        }
    }

    public double AverageExecutionDurationMs
    {
        get;
        set
        {
            if (Math.Abs(field - value) < 1e-9) return;

            field = value;
        }
    }

    public double MaxExecutionDurationMs
    {
        get;
        set
        {
            if (Math.Abs(field - value) < 1e-9) return;

            field = value;
        }
    }
}
