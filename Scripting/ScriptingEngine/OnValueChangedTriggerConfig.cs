using System.Globalization;

namespace Qenex.QSuite.Scripting.ScriptingEngine;

/// <summary>
/// Proti ktere hodnote promenne se trigger vyhodnocuje.
/// </summary>
public enum TriggerValueSource
{
    Raw,
    Eng
}

/// <summary>
/// Typ podminky OnValueChanged triggeru.
/// Delta = |Xn - Xn-1| &gt; threshold; Above = Xn &gt; threshold; Below = Xn &lt; threshold.
/// </summary>
public enum TriggerConditionMode
{
    Delta,
    Above,
    Below
}

/// <summary>
/// Parsovana konfigurace OnValueChanged triggeru z AdditionalInfo
/// (klice <c>source</c>/<c>mode</c>/<c>threshold</c>/<c>hysteresis</c>).
/// Zpetne kompatibilni: chybejici <c>mode</c> =&gt; Delta, chybejici <c>source</c> =&gt; Raw,
/// chybejici <c>threshold</c> =&gt; 0 (tj. delta nad jakoukoli zmenou), chybejici <c>hysteresis</c> =&gt; 0.
/// </summary>
public readonly record struct OnValueChangedTriggerConfig(
    TriggerValueSource Source,
    TriggerConditionMode Mode,
    double Threshold,
    double Hysteresis)
{
    public static bool TryParse(string additionalInfo, out OnValueChangedTriggerConfig config, out string? error)
    {
        config = default;
        error = null;
        var settings = ParseSettings(additionalInfo);

        var source = TriggerValueSource.Raw;
        if (settings.TryGetValue("source", out var sourceText) && !string.IsNullOrWhiteSpace(sourceText))
        {
            switch (sourceText.Trim().ToLowerInvariant())
            {
                case "raw": source = TriggerValueSource.Raw; break;
                case "eng": source = TriggerValueSource.Eng; break;
                default: error = $"unknown source \"{sourceText}\""; return false;
            }
        }

        var mode = TriggerConditionMode.Delta;
        if (settings.TryGetValue("mode", out var modeText) && !string.IsNullOrWhiteSpace(modeText))
        {
            switch (modeText.Trim().ToLowerInvariant())
            {
                case "delta": mode = TriggerConditionMode.Delta; break;
                case "above": mode = TriggerConditionMode.Above; break;
                case "below": mode = TriggerConditionMode.Below; break;
                default: error = $"unknown mode \"{modeText}\""; return false;
            }
        }

        double threshold = 0;
        if (settings.TryGetValue("threshold", out var thresholdText) && !string.IsNullOrWhiteSpace(thresholdText))
        {
            if (!double.TryParse(thresholdText, NumberStyles.Float, CultureInfo.InvariantCulture, out threshold))
            {
                error = $"invalid threshold \"{thresholdText}\"";
                return false;
            }
        }

        double hysteresis = 0;
        if (settings.TryGetValue("hysteresis", out var hysteresisText) && !string.IsNullOrWhiteSpace(hysteresisText))
        {
            if (!double.TryParse(hysteresisText, NumberStyles.Float, CultureInfo.InvariantCulture, out hysteresis))
            {
                error = $"invalid hysteresis \"{hysteresisText}\"";
                return false;
            }
        }

        // U delty je smer zmeny irelevantni, prah bereme jako absolutni velikost.
        if (mode == TriggerConditionMode.Delta)
        {
            threshold = Math.Abs(threshold);
        }

        // Hystereze je vzdy pasmo (absolutni sirka), zaporna hodnota nedava smysl.
        hysteresis = Math.Abs(hysteresis);

        config = new OnValueChangedTriggerConfig(source, mode, threshold, hysteresis);
        return true;
    }

    private static Dictionary<string, string> ParseSettings(string additionalInfo)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(additionalInfo))
        {
            return settings;
        }

        foreach (var part in additionalInfo.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var keyValue = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (keyValue.Length == 2)
            {
                settings[keyValue[0]] = keyValue[1];
            }
        }

        return settings;
    }
}
