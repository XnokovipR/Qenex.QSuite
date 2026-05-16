using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripting.Script;
using Qenex.QSuite.Scripting.ScriptingEngine;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ScriptWrapper : PropertyChangedBase
{
    private readonly IScriptBase script;
    private double lastExecutionInterval;
    private double averageExecutionInterval;
    private double maxExecutionInterval;

    public ScriptWrapper(IScriptBase scriptBase, ScriptingContext? scriptingContext = null)
    {
        script = scriptBase;
        lastExecutionInterval = script.LastExecutionDurationMs;
        averageExecutionInterval = script.AverageExecutionDurationMs;
        maxExecutionInterval = script.MaxExecutionDurationMs;

        if (scriptingContext != null)
        {
            scriptingContext.ScriptExecuted += OnScriptExecuted;
        }
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
            lastExecutionInterval = value;
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
            averageExecutionInterval = value;
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
            maxExecutionInterval = value;
            OnPropertyChanged();
        }
    }

    public bool RefreshExecutionIntervals()
    {
        var changed = false;

        if (Math.Abs(lastExecutionInterval - script.LastExecutionDurationMs) >= 1e-9)
        {
            lastExecutionInterval = script.LastExecutionDurationMs;
            OnPropertyChanged(nameof(LastExecutionInterval));
            changed = true;
        }

        if (Math.Abs(averageExecutionInterval - script.AverageExecutionDurationMs) >= 1e-9)
        {
            averageExecutionInterval = script.AverageExecutionDurationMs;
            OnPropertyChanged(nameof(AverageExecutionInterval));
            changed = true;
        }

        if (Math.Abs(maxExecutionInterval - script.MaxExecutionDurationMs) >= 1e-9)
        {
            maxExecutionInterval = script.MaxExecutionDurationMs;
            OnPropertyChanged(nameof(MaxExecutionInterval));
            changed = true;
        }

        return changed;
    }

    private void OnScriptExecuted(object? sender, ScriptExecutedEventArgs e)
    {
        if (ReferenceEquals(e.Script, script))
        {
            RefreshExecutionIntervals();
        }
    }
}
