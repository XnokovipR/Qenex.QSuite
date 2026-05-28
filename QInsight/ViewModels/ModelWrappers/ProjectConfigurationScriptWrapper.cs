using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripting.Script;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ProjectConfigurationScriptWrapper(IScriptBase script) : PropertyChangedBase
{
    private string originalFileName = script.FileName;
    private bool originalIsEnabled = script.IsEnabled;
    private bool originalIsReplayEnabled = script.IsReplayEnabled;
    private ScriptExecutionMode originalExecutionMode = script.ExecutionMode;
    private bool originalBlocking = script.Blocking;
    private double originalTimeoutMs = script.TimeoutMs;
    private string originalAdditionalInfo = script.AdditionalInfo;

    private string fileName = script.FileName;
    private bool isEnabled = script.IsEnabled;
    private bool isReplayEnabled = script.IsReplayEnabled;
    private ScriptExecutionMode executionMode = script.ExecutionMode;
    private bool blocking = script.Blocking;
    private double timeoutMs = script.TimeoutMs;
    private string additionalInfo = script.AdditionalInfo;

    public string FileName
    {
        get => fileName;
        set
        {
            if (fileName == value)
            {
                return;
            }

            fileName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled == value)
            {
                return;
            }

            isEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool IsReplayEnabled
    {
        get => isReplayEnabled;
        set
        {
            if (isReplayEnabled == value)
            {
                return;
            }

            isReplayEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public ScriptExecutionMode ExecutionMode
    {
        get => executionMode;
        set
        {
            if (executionMode == value)
            {
                return;
            }

            executionMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool Blocking
    {
        get => blocking;
        set
        {
            if (blocking == value)
            {
                return;
            }

            blocking = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public double TimeoutMs
    {
        get => timeoutMs;
        set
        {
            if (Math.Abs(timeoutMs - value) < 1e-9)
            {
                return;
            }

            timeoutMs = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public string AdditionalInfo
    {
        get => additionalInfo;
        set
        {
            if (additionalInfo == value)
            {
                return;
            }

            additionalInfo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool HasChanges =>
        FileName != originalFileName
        || IsEnabled != originalIsEnabled
        || IsReplayEnabled != originalIsReplayEnabled
        || ExecutionMode != originalExecutionMode
        || Blocking != originalBlocking
        || Math.Abs(TimeoutMs - originalTimeoutMs) >= 1e-9
        || AdditionalInfo != originalAdditionalInfo;

    public void ApplyChanges()
    {
        script.FileName = FileName;
        script.IsEnabled = IsEnabled;
        script.IsReplayEnabled = IsReplayEnabled;
        script.ExecutionMode = ExecutionMode;
        script.Blocking = Blocking;
        script.TimeoutMs = TimeoutMs;
        script.AdditionalInfo = AdditionalInfo;

        originalFileName = FileName;
        originalIsEnabled = IsEnabled;
        originalIsReplayEnabled = IsReplayEnabled;
        originalExecutionMode = ExecutionMode;
        originalBlocking = Blocking;
        originalTimeoutMs = TimeoutMs;
        originalAdditionalInfo = AdditionalInfo;

        OnPropertyChanged(nameof(HasChanges));
    }

    public void CancelChanges()
    {
        FileName = originalFileName;
        IsEnabled = originalIsEnabled;
        IsReplayEnabled = originalIsReplayEnabled;
        ExecutionMode = originalExecutionMode;
        Blocking = originalBlocking;
        TimeoutMs = originalTimeoutMs;
        AdditionalInfo = originalAdditionalInfo;
    }
}
