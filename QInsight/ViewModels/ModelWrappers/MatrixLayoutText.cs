using System.Globalization;
using Qenex.QSuite.Variables.QVariables;
using ValueDataType = Qenex.QSuite.Variables.QVariables.Values.ValuesGlobal.ValueDataType;

namespace Qenex.QInsight.ViewModels.ModelWrappers;

/// <summary>
/// Textova podoba layoutu MatrixVariable pro Variable detail v konfiguratoru — stejny vzor
/// key="value";... jako Comm params. Sekce X/Y existuji, kdyz je zadan jejich count; dataCount
/// se pouzije jen bez os (s osami je pocet datovych hodnot odvozeny). Typy jsou volitelne
/// (fallback na Default Data Type promenne), prezentace odkazuji na projektove Presentations.
/// Priklad: xCount="3";xPres="Rpm";yCount="10";yPres="Temperature";dataType="ushort";dataPres="IgnitionAngle"
/// </summary>
internal static class MatrixLayoutText
{
    private static readonly string[] KnownKeys =
        ["xCount", "xType", "xPres", "xLabel", "yCount", "yType", "yPres", "yLabel", "dataCount", "dataType", "dataPres"];

    public static string Build(MatrixVariable variable)
    {
        var parts = new List<string>();

        AppendSection(parts, "x", variable.XAxis, variable.XAxis?.Count ?? 0);
        AppendSection(parts, "y", variable.YAxis, variable.YAxis?.Count ?? 0);

        if (variable.XAxis == null)
        {
            parts.Add($"dataCount=\"{variable.Data.Count}\"");
        }

        if (variable.Data.DataType != ValueDataType.Undefined)
        {
            parts.Add($"dataType=\"{ToTypeText(variable.Data.DataType)}\"");
        }

        if (!string.IsNullOrEmpty(variable.Data.Presentation?.Name))
        {
            parts.Add($"dataPres=\"{variable.Data.Presentation.Name}\"");
        }

        return string.Join(";", parts);
    }

    public static MatrixLayoutParts Parse(string layout)
    {
        var settings = (layout ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1].Trim('"'), StringComparer.OrdinalIgnoreCase);

        var unknownKey = settings.Keys.FirstOrDefault(key => !KnownKeys.Contains(key, StringComparer.OrdinalIgnoreCase));
        if (unknownKey != null)
        {
            throw new ArgumentException($"Unknown layout key '{unknownKey}' (expected {string.Join(", ", KnownKeys)}).");
        }

        var x = ParseSection(settings, "x");
        var y = ParseSection(settings, "y");

        int dataCount;
        if (x == null)
        {
            if (!settings.TryGetValue("dataCount", out var dataCountText) ||
                !int.TryParse(dataCountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out dataCount) ||
                dataCount < 1)
            {
                throw new ArgumentException("A matrix without axes needs dataCount=\"<number of values>\".");
            }
        }
        else
        {
            // With axes the data count is derived (xCount, or xCount * yCount).
            dataCount = x.Count * Math.Max(y?.Count ?? 1, 1);
        }

        if (y != null && x == null)
        {
            throw new ArgumentException("Y axis (yCount) requires X axis (xCount).");
        }

        var data = new MatrixSectionParts(
            dataCount,
            ParseType(settings, "dataType"),
            settings.GetValueOrDefault("dataPres", string.Empty),
            string.Empty);

        return new MatrixLayoutParts(x, y, data);
    }

    private static void AppendSection(List<string> parts, string prefix, MatrixSection? section, int count)
    {
        if (section == null)
        {
            return;
        }

        parts.Add($"{prefix}Count=\"{count}\"");
        if (section.DataType != ValueDataType.Undefined)
        {
            parts.Add($"{prefix}Type=\"{ToTypeText(section.DataType)}\"");
        }

        if (!string.IsNullOrEmpty(section.Presentation?.Name))
        {
            parts.Add($"{prefix}Pres=\"{section.Presentation.Name}\"");
        }

        if (!string.IsNullOrEmpty(section.Label))
        {
            parts.Add($"{prefix}Label=\"{section.Label}\"");
        }
    }

    private static MatrixSectionParts? ParseSection(IReadOnlyDictionary<string, string> settings, string prefix)
    {
        if (!settings.TryGetValue($"{prefix}Count", out var countText))
        {
            if (settings.ContainsKey($"{prefix}Type") || settings.ContainsKey($"{prefix}Pres") ||
                settings.ContainsKey($"{prefix}Label"))
            {
                throw new ArgumentException($"{prefix}Type/{prefix}Pres/{prefix}Label given without {prefix}Count.");
            }

            return null;
        }

        if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count < 1)
        {
            throw new ArgumentException($"Invalid {prefix}Count '{countText}' (a positive number of values).");
        }

        return new MatrixSectionParts(count, ParseType(settings, $"{prefix}Type"),
            settings.GetValueOrDefault($"{prefix}Pres", string.Empty),
            settings.GetValueOrDefault($"{prefix}Label", string.Empty));
    }

    private static ValueDataType ParseType(IReadOnlyDictionary<string, string> settings, string key)
    {
        if (!settings.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text))
        {
            return ValueDataType.Undefined;
        }

        if (!Enum.TryParse<ValueDataType>(text, ignoreCase: true, out var type) ||
            type is ValueDataType.Undefined or ValueDataType.String)
        {
            throw new ArgumentException($"Invalid {key} '{text}' (a numeric data type, e.g. byte, ushort, float).");
        }

        return type;
    }

    private static string ToTypeText(ValueDataType type)
    {
        return type.ToString().ToLowerInvariant();
    }
}

internal sealed record MatrixSectionParts(int Count, ValueDataType DataType, string PresentationName, string Label);

internal sealed record MatrixLayoutParts(MatrixSectionParts? X, MatrixSectionParts? Y, MatrixSectionParts Data);
