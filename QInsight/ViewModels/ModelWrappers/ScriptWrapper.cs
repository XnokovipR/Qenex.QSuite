using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripts.PythonScript;
using Qenex.QSuite.Scripts.Script;

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
        get => script.LastExecutionInterval;
        set
        {
            if (Math.Abs(script.LastExecutionInterval - value) < 1e-9) return;
            script.LastExecutionInterval = value;
            OnPropertyChanged();
        }
    }

    public double AverageExecutionInterval
    {
        get => script.AverageExecutionInterval;
        set
        {
            if (Math.Abs(script.AverageExecutionInterval - value) < 1e-9) return;
            script.AverageExecutionInterval = value;
            OnPropertyChanged();
        }
    }

    public double MaxExecutionInterval
    {
        get => script.MaxExecutionInterval;
        set
        {            
            if (Math.Abs(script.MaxExecutionInterval - value) < 1e-9) return;
            script.MaxExecutionInterval = value;
            OnPropertyChanged();
        }
    }
    
    
}