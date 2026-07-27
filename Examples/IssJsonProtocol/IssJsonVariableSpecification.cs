using Qenex.QSuite.Protocols.Protocol;

namespace Qenex.QSuite.Examples.IssJsonProtocol;

/// <summary>
/// Addressing of one variable: the name of the JSON field whose value the variable receives.
/// The property matches the commParam key 1:1 ("path"), so the inherited reflection-based
/// ToCommParam() round-trips the specification without an override.
/// </summary>
public class IssJsonVariableSpecification : ProtVariableSpecification
{
    public IssJsonVariableSpecification()
    {
        Name = "IssJsonVariableSpecification";
    }

    /// <summary>Name of the field in the received JSON object, e.g. "latitude". Case-sensitive.</summary>
    public string Path { get; set; } = string.Empty;

    public static IssJsonVariableSpecification Create(string commParams)
    {
        var parameters = commParams
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(parameter => parameter.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim().Trim('"'), StringComparer.OrdinalIgnoreCase);

        if (!parameters.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Missing mandatory commParam 'path'.");
        }

        return new IssJsonVariableSpecification { Path = path };
    }
}
