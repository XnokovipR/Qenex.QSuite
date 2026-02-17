using Qenex.QSuite.Scripts.Script;

namespace Qenex.QSuite.Scripts.PythonScript;

public class PyScript : IScriptBase
{
    private int executionCount;
    public string FileName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

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