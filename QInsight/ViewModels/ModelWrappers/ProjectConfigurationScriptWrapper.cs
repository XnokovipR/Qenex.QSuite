using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripting.Script;
using System.Globalization;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

public class ProjectConfigurationScriptWrapper(IScriptBase script, bool isNew = false) : PropertyChangedBase
{
    private static readonly IReadOnlyList<string> PeriodicUnitValues = ["ms", "s", "min", "h"];
    private const string DefaultPeriodicParameters = "period=1;unit=s";
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

    public IScriptBase Script => script;
    public bool IsNew { get; } = isNew;
    public string OriginalFileName => originalFileName;
    public IReadOnlyList<string> PeriodicUnitOptions => PeriodicUnitValues;

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
            UpdateParametersForExecutionMode();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPeriodic));
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool IsPeriodic => ExecutionMode == ScriptExecutionMode.Periodic;

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
            value ??= string.Empty;
            if (additionalInfo == value)
            {
                return;
            }

            additionalInfo = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PeriodicPeriod));
            OnPropertyChanged(nameof(PeriodicUnit));
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public double PeriodicPeriod
    {
        get
        {
            var settings = ParseAdditionalInfo(AdditionalInfo);
            return settings.TryGetValue("period", out var value)
                   && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var period)
                ? period
                : 1;
        }
        set
        {
            if (value <= 0 || Math.Abs(PeriodicPeriod - value) < 1e-9)
            {
                return;
            }

            UpdateAdditionalInfoParameter("period", value.ToString(CultureInfo.InvariantCulture));
        }
    }

    public string PeriodicUnit
    {
        get
        {
            var settings = ParseAdditionalInfo(AdditionalInfo);
            return settings.TryGetValue("unit", out var value)
                ? NormalizePeriodicUnit(value)
                : "s";
        }
        set
        {
            value = NormalizePeriodicUnit(value);
            if (PeriodicUnit == value)
            {
                return;
            }

            UpdateAdditionalInfoParameter("unit", value);
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

    private void UpdateParametersForExecutionMode()
    {
        AdditionalInfo = executionMode == ScriptExecutionMode.Periodic
            ? DefaultPeriodicParameters
            : string.Empty;
    }

    private void UpdateAdditionalInfoParameter(string key, string value)
    {
        var settings = ParseAdditionalInfo(AdditionalInfo);
        settings[key] = value;
        AdditionalInfo = FormatAdditionalInfo(settings);
    }

    private static Dictionary<string, string> ParseAdditionalInfo(string additionalInfo)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parts = additionalInfo.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var keyValue = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (keyValue.Length == 2)
            {
                settings[keyValue[0]] = keyValue[1];
            }
        }

        return settings;
    }

    private static string FormatAdditionalInfo(IReadOnlyDictionary<string, string> settings)
    {
        var orderedKeys = new[] { "period", "unit" };
        var parameters = orderedKeys
            .Where(settings.ContainsKey)
            .Select(key => $"{key}={settings[key]}")
            .ToList();

        parameters.AddRange(settings
            .Where(pair => !orderedKeys.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
            .Select(pair => $"{pair.Key}={pair.Value}"));

        return string.Join(";", parameters);
    }

    private static string NormalizePeriodicUnit(string? unit)
    {
        return unit?.Trim().ToLowerInvariant() switch
        {
            "ms" or "msec" or "millisec" or "millisecond" or "milliseconds" => "ms",
            "s" or "sec" or "second" or "seconds" => "s",
            "m" or "min" or "minute" or "minutes" => "min",
            "h" or "hour" or "hours" => "h",
            _ => "s"
        };
    }
}
