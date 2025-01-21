using System.Runtime.CompilerServices;


//[assembly: InternalsVisibleTo("Qenex.QSuite.Variables.CreateVariableTest")]

namespace Qenex.QSuite.QVariables;

/// <summary>
/// Global definitions and methods for variables.
/// </summary>
public class VariablesGlobal
{
    /// <summary>
    /// Enumerable of variable types.
    /// </summary>
    public enum VariableType { Undefined = 0, Scalar, Curve, Map, Array, String }
    
    /// <summary>
    /// Dictionary of variable types and their corresponding string representation.
    /// </summary>
    public static readonly Dictionary<VariableType, string> VariableTypeDict = new Dictionary<VariableType, string>()
    {
        { VariableType.Scalar, "Qenex.QSuite.QVariables.ScalarVariable" },
        { VariableType.String, "Qenex.QSuite.QVariables.StringVariable" },
        { VariableType.Array, "Qenex.QSuite.QVariables.ArrayVariable" },
        { VariableType.Curve, "Qenex.QSuite.QVariables.CurveVariable" },
        { VariableType.Map, "Qenex.QSuite.QVariables.MapVariable" }
    };  
    
    /// <summary>
    /// Create instance of IVariableBase based on VariableType.
    /// </summary>
    /// <param name="variableType">Enumerable variable type.</param>
    /// <returns>Created instance of variable.</returns>
    /// /// <exception cref="ArgumentException">Exception is thrown when either a type is not found or instance cannot be created.</exception>
    public static IVariableBase CreateInstance(VariableType variableType)
    {
        if (!VariableTypeDict.TryGetValue(variableType, out var stringType))
        {
            throw new ArgumentException($"Type {variableType} not found in dictionary.");
        }
        
        var type = Type.GetType(stringType);
        if (type == null)
        {
            throw new ArgumentException($"Type {stringType} not found.");
        }

        if (Activator.CreateInstance(type) is not IVariableBase createdInstance)
        {
            throw new ArgumentException($"Instance of {type} not created.");
        }
        
        return createdInstance;
    }
}