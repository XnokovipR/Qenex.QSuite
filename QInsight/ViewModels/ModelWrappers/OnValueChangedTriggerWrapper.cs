using System.Globalization;
using Qenex.QLibs.QUI;
using Qenex.QSuite.Scripting.ScriptingEngine;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

/// <summary>
/// Editovatelny radek jednoho OnValueChanged triggeru nad promennou. Sklada/parsuje
/// AdditionalInfo string (klice <c>mode</c>/<c>threshold</c>/<c>source</c>).
/// </summary>
public class OnValueChangedTriggerWrapper : PropertyChangedBase
{
    private readonly Func<IEnumerable<string>> scriptOptionsProvider;
    private readonly Action onChanged;
    private string selectedScriptFileName;
    private TriggerValueSource source;
    private TriggerConditionMode mode;
    private string threshold;
    private string hysteresis;

    public OnValueChangedTriggerWrapper(
        string selectedScriptFileName,
        TriggerValueSource source,
        TriggerConditionMode mode,
        string threshold,
        string hysteresis,
        Func<IEnumerable<string>> scriptOptionsProvider,
        Action onChanged)
    {
        this.selectedScriptFileName = selectedScriptFileName;
        this.source = source;
        this.mode = mode;
        this.threshold = threshold;
        this.hysteresis = hysteresis;
        this.scriptOptionsProvider = scriptOptionsProvider;
        this.onChanged = onChanged;
    }

    public IEnumerable<string> ScriptOptions => scriptOptionsProvider();
    public IEnumerable<TriggerValueSource> SourceOptions => Enum.GetValues<TriggerValueSource>();
    public IEnumerable<TriggerConditionMode> ModeOptions => Enum.GetValues<TriggerConditionMode>();

    public string SelectedScriptFileName
    {
        get => selectedScriptFileName;
        set
        {
            if (selectedScriptFileName == value)
            {
                return;
            }

            selectedScriptFileName = value;
            OnPropertyChanged();
            onChanged();
        }
    }

    public TriggerValueSource Source
    {
        get => source;
        set
        {
            if (source == value)
            {
                return;
            }

            source = value;
            OnPropertyChanged();
            onChanged();
        }
    }

    public TriggerConditionMode Mode
    {
        get => mode;
        set
        {
            if (mode == value)
            {
                return;
            }

            mode = value;
            OnPropertyChanged();
            onChanged();
        }
    }

    public string Threshold
    {
        get => threshold;
        set
        {
            if (threshold == value)
            {
                return;
            }

            threshold = value;
            OnPropertyChanged();
            onChanged();
        }
    }

    /// <summary>Pasmo hysereze (absolutni sirka ve stejne jednotce jako source). Plati jen pro Above/Below.</summary>
    public string Hysteresis
    {
        get => hysteresis;
        set
        {
            if (hysteresis == value)
            {
                return;
            }

            hysteresis = value;
            OnPropertyChanged();
            onChanged();
        }
    }

    public void RefreshScriptOptions()
    {
        OnPropertyChanged(nameof(ScriptOptions));
    }

    public bool ReferencesScript(string fileName)
    {
        return string.Equals(SelectedScriptFileName, fileName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Slozeny AdditionalInfo string pro <see cref="OnValueChangedScriptTrigger"/>.</summary>
    public string BuildAdditionalInfo()
    {
        var thresholdValue = double.TryParse(threshold, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedThreshold)
            ? parsedThreshold
            : 0;
        var hysteresisValue = double.TryParse(hysteresis, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedHysteresis)
            ? parsedHysteresis
            : 0;

        return string.Join(";",
            $"mode={mode.ToString().ToLowerInvariant()}",
            $"threshold={thresholdValue.ToString(CultureInfo.InvariantCulture)}",
            $"hysteresis={hysteresisValue.ToString(CultureInfo.InvariantCulture)}",
            $"source={source.ToString().ToLowerInvariant()}");
    }

    public static OnValueChangedTriggerWrapper FromTrigger(
        OnValueChangedScriptTrigger trigger,
        Func<IEnumerable<string>> scriptOptionsProvider,
        Action onChanged)
    {
        OnValueChangedTriggerConfig.TryParse(trigger.AdditionalInfo, out var config, out _);
        return new OnValueChangedTriggerWrapper(
            trigger.ScriptFileName,
            config.Source,
            config.Mode,
            config.Threshold.ToString(CultureInfo.InvariantCulture),
            config.Hysteresis.ToString(CultureInfo.InvariantCulture),
            scriptOptionsProvider,
            onChanged);
    }
}
