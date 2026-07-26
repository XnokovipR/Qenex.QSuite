using System.Globalization;
using System.Reflection;
using Qenex.QSuite.LogSystems.LogSystem;
using Qenex.QSuite.Variables.VariableEvents;

namespace Qenex.QSuite.Protocols.Protocol;

public abstract class ProtVariableSpecification : IProtVariableSpecification
{
    public string Name { get; set; } = string.Empty;

    // Reflection-based default: a "CommParams" property wins as the raw string; otherwise the
    // common Direction/VariableEvent(eventRef)/Multiplier keys come first and every remaining
    // readable property is emitted camelCase. Specifications whose properties do not match
    // their commParam keys 1:1 must override with an explicit serialization.
    public virtual string ToCommParam()
    {
        var properties = GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead)
            .ToList();

        if (properties.FirstOrDefault(property => property.Name == "CommParams")?.GetValue(this) is string commParams)
        {
            return commParams;
        }

        var parameters = new List<string>();
        AddCommParam(parameters, properties, "Direction", value => value.ToString()!.ToLowerInvariant());
        AddCommParam(parameters, properties, "VariableEvent", value => ((IVarEvent)value).Name, "eventRef");
        AddCommParam(parameters, properties, "Multiplier");

        foreach (var property in properties.Where(property => property.Name is not ("Name" or "Direction" or "VariableEvent" or "Multiplier")))
        {
            var text = Convert.ToString(property.GetValue(this), CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            parameters.Add($"{ToCamelCase(property.Name)}=\"{text}\"");
        }

        return string.Join(";", parameters);
    }

    private void AddCommParam(
        ICollection<string> parameters,
        IEnumerable<PropertyInfo> properties,
        string propertyName,
        Func<object, string>? valueFormatter = null,
        string? parameterName = null)
    {
        var property = properties.FirstOrDefault(property => property.Name == propertyName);
        var value = property?.GetValue(this);
        if (value == null)
        {
            return;
        }

        var text = valueFormatter?.Invoke(value) ?? Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        parameters.Add($"{parameterName ?? ToCamelCase(propertyName)}=\"{text}\"");
    }

    private static string ToCamelCase(string value)
    {
        return string.IsNullOrEmpty(value)
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
    }
}