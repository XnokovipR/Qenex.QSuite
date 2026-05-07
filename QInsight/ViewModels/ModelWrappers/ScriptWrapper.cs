using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripting.Script;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ScriptWrapper : PropertyChangedBase
{
    private readonly IScriptBase script;

    public ScriptWrapper(IScriptBase scriptBase)
    {
        script = scriptBase;
    }
    
    public IScriptBase  Script => script;

    public string FileName
    {
        get => script.FileName;
        set
        {
            if (script.FileName == value) return;
            script.FileName = value;
            OnPropertyChanged();
        }
    }

    public string Content
    {
        get => script.Content;
        set
        {
            if (script.Content == value) return;
            script.Content = value;
            OnPropertyChanged();
        }
    }

    public ScriptExecutionMode ExecutionMode
    {
        get => script.ExecutionMode;
        set
        {
            if (script.ExecutionMode == value) return;
            script.ExecutionMode = value;
            OnPropertyChanged();
        }
    }

    public string AdditionalInfo
    {
        get => script.AdditionalInfo;
        set
        {
            if (script.AdditionalInfo == value) return;
            script.AdditionalInfo = value;
            OnPropertyChanged();
        }
    }
 
    public double LastExecutionInterval
    {
        get => script.LastExecutionDurationMs;
        set
        {
            if (Math.Abs(script.LastExecutionDurationMs - value) < 1e-9) return;
            script.LastExecutionDurationMs = value;
            OnPropertyChanged();
        }
    }

    public double AverageExecutionInterval
    {
        get => script.AverageExecutionDurationMs;
        set
        {
            if (Math.Abs(script.AverageExecutionDurationMs - value) < 1e-9) return;
            script.AverageExecutionDurationMs = value;
            OnPropertyChanged();
        }
    }

    public double MaxExecutionInterval
    {
        get => script.MaxExecutionDurationMs;
        set
        {            
            if (Math.Abs(script.MaxExecutionDurationMs - value) < 1e-9) return;
            script.MaxExecutionDurationMs = value;
            OnPropertyChanged();
        }
    }
    
    
}